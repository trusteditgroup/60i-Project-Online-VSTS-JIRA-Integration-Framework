using System;
using System.Configuration;
using System.Web.Http.Cors;
using System.Web.Mvc;
using Autofac;
using Autofac.Integration.Mvc;
using NLog;
using ProjectOnlineSystemConnector.BusinessServices;
using ProjectOnlineSystemConnector.DataAccess.Database.UnitOfWork;
using ProjectOnlineSystemConnector.DataModel.Common;
using static System.Boolean;

namespace ProjectOnlineSystemConnector.Web.Controllers
{
    [EnableCors("*", "*", "*")]
    public class BaseController : Controller
    {
        protected MasterBusinessService MasterBusinessService { get; }
        protected CommonBusinessService CommonBusinessService { get; }
        protected SyncSystemBusinessService SyncSystemBusinessService { get; }
        protected ProjectServerSystemLinkBusinessService ProjectServerSystemLinkBusinessService { get; }
        protected SyncSystemFieldMappingBusinessService SyncSystemFieldMappingBusinessService { get; }
        protected MasterWorklogBusinessService MasterWorklogBusinessService { get; }

        protected Logger Logger { get; }
        protected bool IsDebugMode { get; }
        protected UnitOfWork UnitOfWork { get; }

        public BaseController()
        {
            //ProjectServerSystemLinkBusinessService = new ProjectServerSystemLinkBusinessService();
            Logger = LogManager.GetCurrentClassLogger();
            IsDebugMode = Parse(ConfigurationManager.AppSettings["IsDebugMode"]);
            UnitOfWork = new UnitOfWork();
            CommonBusinessService = new CommonBusinessService(UnitOfWork);
            MasterBusinessService = new MasterBusinessService(UnitOfWork);
            SyncSystemBusinessService = new SyncSystemBusinessService(UnitOfWork);
            ProjectServerSystemLinkBusinessService = AutofacDependencyResolver.Current
                .ApplicationContainer.Resolve<ProjectServerSystemLinkBusinessService>();
            SyncSystemFieldMappingBusinessService = new SyncSystemFieldMappingBusinessService(UnitOfWork);
            MasterWorklogBusinessService = new MasterWorklogBusinessService(UnitOfWork);
        }

        public JsonResult HandleException(Exception exception)
        {
            Logger.Fatal(exception);
            return HandleException(exception.Message + Environment.NewLine + exception.StackTrace, false);
        }

        public JsonResult HandleException(string message, bool needLog)
        {
            if (needLog)
            {
                Logger.Fatal(message);
            }
            //if (IsDebugMode)
            //{
            //    return Json(new ProxyResponse
            //    {
            //        Result = "ko",
            //        Data = message
            //    }, JsonRequestBehavior.AllowGet);
            //}
            return Json(new ProxyResponse
            {
                Result = "ko",
                Data = $"An error occured. Please contact administrator for details. Error date {DateTime.Now}"
            }, JsonRequestBehavior.AllowGet);
        }
    }
}