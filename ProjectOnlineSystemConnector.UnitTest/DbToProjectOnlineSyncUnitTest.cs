using System;
using System.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLog;
using ProjectOnlineSystemConnector.DataAccess.CSOM;
using ProjectOnlineSystemConnector.DataAccess.Database.UnitOfWork;
using ProjectOnlineSystemConnector.SignalR;
using ProjectOnlineSystemConnector.SyncServices;

namespace ProjectOnlineSystemConnector.UnitTest
{
    [TestClass]
    public class DbToProjectOnlineSyncUnitTest : BaseTest
    {
        private int publishMax, stagingRecordLifeTime, projectsPerIteration;
        private DbToProjectOnlineSync dbToProjectOnlineSync;
        private ProjectOnlineAccessService projectOnlineAccessService;
        private ProjectOnlineODataService projectOnlineODataService;

        [TestMethod]
        public void SynchronizeDataTestMethod()
        {
            Logger logger = LogManager.GetCurrentClassLogger();
            try
            {
                ProjectOnlineSystemConnectorHubHelper.StartHubConnection(ConfigurationManager.AppSettings["SignalRHostUrl"]);

                publishMax = int.Parse(ConfigurationManager.AppSettings["PublishMax"]);
                stagingRecordLifeTime = int.Parse(ConfigurationManager.AppSettings["StagingRecordLifeTime"]);
                projectsPerIteration = int.Parse(ConfigurationManager.AppSettings["ProjectsPerIteration"]);

                InitProjectOnlineAccessService();

                using (var unitOfWork = new UnitOfWork())
                {
                    Guid winServiceIterationUid = Guid.NewGuid();
                    dbToProjectOnlineSync = new DbToProjectOnlineSync(unitOfWork, projectOnlineODataService, publishMax,
                        stagingRecordLifeTime, projectsPerIteration, winServiceIterationUid);
                    dbToProjectOnlineSync.SynchronizeData(null);
                }
            }
            catch (Exception exception)
            {
                logger.Error(exception);
            }
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
    }
}
