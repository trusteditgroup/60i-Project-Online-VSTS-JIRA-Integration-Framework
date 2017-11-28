using System;
using System.Configuration;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Http;
using Autofac;
using Autofac.Integration.Mvc;
using AutoMapper;
using NLog;
using ProjectOnlineSystemConnector.BusinessServices;
using ProjectOnlineSystemConnector.Data.Views.EntityClasses;
using ProjectOnlineSystemConnector.DataAccess.CSOM;
using ProjectOnlineSystemConnector.DataAccess.Database.UnitOfWork;
using ProjectOnlineSystemConnector.DataModel.DTO;

namespace ProjectOnlineSystemConnector.Web
{
    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            ConfigureMapping();
            ConfigureAutofac();
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

        private void ConfigureAutofac()
        {
            bool isProjectOnline = Boolean.Parse(ConfigurationManager.AppSettings["IsProjectOnline"]);
            var builder = new ContainerBuilder();
            builder.RegisterControllers(typeof(MvcApplication).Assembly);
            builder.RegisterType<UnitOfWork>();
            builder.Register(c => new ProjectOnlineODataService(ConfigurationManager
                    .AppSettings["ProjectOnlineUrl"], ConfigurationManager.AppSettings["ProjectOnlineUserName"],
                ConfigurationManager.AppSettings["ProjectOnlinePassword"], isProjectOnline));
            builder.RegisterType<ProjectServerSystemLinkBusinessService>();
            builder.RegisterType<SyncSystemBusinessService>();
            builder.RegisterType<SyncSystemFieldMappingBusinessService>();
            builder.RegisterType<SyncSystemSettingBusinessService>();
            builder.RegisterType<SyncSystemSettingValueBusinessService>();
            builder.RegisterType<SyncSystemTypeBusinessService>();

            IContainer container = builder.Build();
            DependencyResolver.SetResolver(new AutofacDependencyResolver(container));
        }

        private void Application_Error(object sender, EventArgs e)
        {
            Exception exception = Server.GetLastError();
            HttpException httpException = exception as HttpException;
            Response.Clear();
            try
            {
                ILogger logger = DependencyResolver.Current.GetService<ILogger>();
                logger.Error(exception);

                string action;
                if (httpException != null)
                {
                    switch (httpException.GetHttpCode())
                    {
                        case 401:
                            action = "Unauthorized";
                            break;
                        case 403:
                            action = "AccessDenied";
                            break;
                        case 404:
                            action = "NotFound";
                            break;
                        default:
                            action = "GeneralError";
                            break;
                    }
                }
                else
                    action = "GeneralError";

                Response.TrySkipIisCustomErrors = true;
                Response.Redirect($"/Errors/{action}", true);

                Server.ClearError();
            }
            catch
            {
                // ignored
            }
        }
    }
}