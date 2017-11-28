using System;
using System.Configuration;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using AutoMapper;
using NLog;
using ProjectOnlineSystemConnector.BusinessServices;
using ProjectOnlineSystemConnector.Data.Views.EntityClasses;
using ProjectOnlineSystemConnector.DataAccess.CSOM;
using ProjectOnlineSystemConnector.DataAccess.Database.UnitOfWork;
using ProjectOnlineSystemConnector.DataModel.DTO;
using ProjectOnlineSystemConnector.SignalR;
using ProjectOnlineSystemConnector.SyncServices;


namespace ProjectOnlineSystemConnector.WinService
{
    partial class ProjectOnlineSystemConnectorWinService : ServiceBase
    {
        private Timer timerJira;

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private static readonly object lockObjectJira = new object();
        private DbToProjectOnlineSync dbToProjectOnlineSync;
        private ProjectOnlineAccessService projectOnlineAccessService;
        private ProjectOnlineODataService projectOnlineODataService;
        private int publishMax, stagingRecordLifeTime, projectsPerIteration, periodCleanCache;

        private DateTime lastCleanCache = DateTime.Now;

        public ProjectOnlineSystemConnectorWinService()
        {
            InitializeComponent();
            ConfigureMapping();
        }

        private void ConfigureMapping()
        {
            Mapper.Initialize(cfg =>
            {
                cfg.CreateMap<SyncSystem, SyncSystemDTO>();
                cfg.CreateMap<SyncSystemDTO, SyncSystem>();
                cfg.CreateMap<SyncSystemFieldMapping, SyncSystemFieldMappingDTO>();
                cfg.CreateMap<SyncSystemFieldMappingDTO, SyncSystemFieldMapping>();
                cfg.CreateMap<SyncSystemSetting, SyncSystemSettingDTO>();
                cfg.CreateMap<SyncSystemSettingDTO, SyncSystemSetting>();
                cfg.CreateMap<SyncSystemSettingValue, SyncSystemSettingValueDTO>();
                cfg.CreateMap<SyncSystemSettingValueDTO, SyncSystemSettingValue>();
                cfg.CreateMap<SyncSystemType, SyncSystemTypeDTO>();
                cfg.CreateMap<SyncSystemTypeDTO, SyncSystemType>();
                cfg.CreateMap<ProjectServerSystemLink, ProjectServerSystemLinkDTO>();
                cfg.CreateMap<ProjectServerSystemLinkDTO, ProjectServerSystemLink>();
                cfg.CreateMap<VProjectServerSystemLink, ProjectServerSystemLinkDTO>();
                cfg.CreateMap<ProjectServerSystemLinkDTO, VProjectServerSystemLink>();
            });
        }

        protected override void OnStart(string[] args)
        {
            logger.Info("ProjectOnlineSystemConnectorWinService.OnStart START");
            try
            {
                ProjectOnlineSystemConnectorHubHelper.StartHubConnection(ConfigurationManager.AppSettings["SignalRHostUrl"]);
                Task.Run(StartTimerJira);
            }
            catch (Exception exception)
            {
                logger.Fatal(exception);
            }
            logger.Info("ProjectOnlineSystemConnectorWinService.OnStart END");
        }

        private async Task StartTimerJira()
        {
            logger.Info("StartTimerJira START");
            await Task.Delay(10 * 1000);
            try
            {
                logger.Info("StartTimerJira InitProjectOnlineAccessService START");

                publishMax = int.Parse(ConfigurationManager.AppSettings["PublishMax"]);
                stagingRecordLifeTime = int.Parse(ConfigurationManager.AppSettings["StagingRecordLifeTime"]);
                projectsPerIteration = int.Parse(ConfigurationManager.AppSettings["ProjectsPerIteration"]);
                periodCleanCache = Int32.Parse(ConfigurationManager.AppSettings["TimerIntervalMinutesCleanCache"]) * 60 * 1000;

                InitProjectOnlineAccessService();
                logger.Info("StartTimerJira InitProjectOnlineAccessService END");

                int period = Int32.Parse(ConfigurationManager.AppSettings["TimerIntervalMinutesJira"]) * 60 * 1000;
                lastCleanCache = DateTime.Now;
                logger.Info($"StartTimerJira period: {period}");
                timerJira = new Timer(OnTimerJiraTick, null, 2000, period);
            }
            catch (Exception exception)
            {
                logger.Fatal(exception);
            }
            logger.Info("StartTimerJira END");
        }

        private void InitProjectOnlineAccessService()
        {
            bool isProjectOnline = Boolean.Parse(ConfigurationManager.AppSettings["IsProjectOnline"]);

            projectOnlineAccessService = new ProjectOnlineAccessService(ConfigurationManager
                    .AppSettings["ProjectOnlineUrl"], ConfigurationManager.AppSettings["ProjectOnlineUserName"],
                ConfigurationManager.AppSettings["ProjectOnlinePassword"], isProjectOnline, Guid.Empty);

            projectOnlineODataService = new ProjectOnlineODataService(ConfigurationManager
                    .AppSettings["ProjectOnlineUrl"], ConfigurationManager.AppSettings["ProjectOnlineUserName"],
                ConfigurationManager.AppSettings["ProjectOnlinePassword"], isProjectOnline);

            projectOnlineAccessService.InitCache();
            projectOnlineODataService.InitCache();
        }

        private void OnTimerJiraTick(object state)
        {
            TimeSpan timeElapsed = DateTime.Now - lastCleanCache;
            logger.Info($"OnTimerJiraTick START lastCleanCache: {lastCleanCache}; " +
                        $"timeElapsed.TotalMilliseconds: {timeElapsed.TotalMilliseconds}");
            if (Monitor.TryEnter(lockObjectJira))
            {
                try
                {
                    if (timeElapsed.TotalMilliseconds >= periodCleanCache
                        || ProjectOnlineCache.NeedInitCache())
                    {
                        projectOnlineAccessService.ClearCache();
                        projectOnlineAccessService.InitCache();
                        projectOnlineODataService.ClearCache();
                        projectOnlineODataService.InitCache();
                        lastCleanCache = DateTime.Now;
                    }
                    using (var unitOfWork = new UnitOfWork())
                    {
                        Guid winServiceIterationUid = Guid.NewGuid();
                        dbToProjectOnlineSync = new DbToProjectOnlineSync(unitOfWork, projectOnlineODataService, publishMax,
                            stagingRecordLifeTime, projectsPerIteration, winServiceIterationUid);
                        dbToProjectOnlineSync.SynchronizeData(state);
                    }
                }
                catch (Exception exception)
                {
                    logger.Fatal(exception);
                }
                finally
                {
                    Monitor.Exit(lockObjectJira);
                }
            }
            logger.Info("OnTimerJiraTick END");
        }

        protected override void OnStop()
        {
            try
            {
                logger.Info("ProjectOnlineSystemConnectorWinService.OnStop START");
                //dbToProjectOnlineSync?.StopProcessWebHookEntries();
                if (timerJira != null)
                {
                    timerJira.Dispose();
                    timerJira = null;
                }
                logger.Info("ProjectOnlineSystemConnectorWinService.OnStop END");
            }
            catch (Exception exception)
            {
                logger.Error(exception);
            }
        }
    }
}