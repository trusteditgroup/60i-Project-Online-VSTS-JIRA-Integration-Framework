using System;
using System.Configuration;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Mvc;
using ProjectOnlineSystemConnector.DataAccess.Jira;
using ProjectOnlineSystemConnector.DataModel.Common;
using ProjectOnlineSystemConnector.DataModel.DTO;
using ProjectOnlineSystemConnector.SyncServices;

namespace ProjectOnlineSystemConnector.Web.Controllers
{
    [EnableCors("*", "*", "*")]
    public class JiraController : BaseController
    {
        public JsonResult GetTest()
        {
            Logger.Info("GetTest STARTED");
            return Json(new ProxyResponse
            {
                Result = "ok",
                Data = "GetTest FINISH",
            }, JsonRequestBehavior.AllowGet);
        }

        //////Button to click on the PDP page
        ////public JsonResult ButtonMergeDbToEpm()
        ////{
        ////    Logger.Info("ButtonMergeDbToEpm START");
        ////    try
        ////    {
        ////        int publishMax = int.Parse(ConfigurationManager.AppSettings["PublishMax"]);
        ////        int projectsPerIteration = int.Parse(ConfigurationManager.AppSettings["ProjectsPerIteration"]);
        ////        int stagingRecordLifeTime = int.Parse(ConfigurationManager.AppSettings["StagingRecordLifeTime"]);

        ////        bool isProjectOnline = Boolean.Parse(ConfigurationManager.AppSettings["IsProjectOnline"]);
        ////        var projectOnlineAccessService = new ProjectOnlineAccessService(ConfigurationManager
        ////            .AppSettings["ProjectOnlineUrl"], ConfigurationManager.AppSettings["ProjectOnlineUserName"],
        ////            ConfigurationManager.AppSettings["ProjectOnlinePassword"], isProjectOnline);
        ////        projectOnlineAccessService.InitCache();

        ////        var dataSync = new DataSyncJira2(ConfigurationManager.AppSettings["ProjectOnlineUrl"], publishMax,
        ////            stagingRecordLifeTime, projectsPerIteration);
        ////        dataSync.ProcessWebHookEntries(projectOnlineAccessService);
        ////    }
        ////    catch (Exception exception)
        ////    {
        ////        HandleException(exception);
        ////    }
        ////    Logger.Info("ButtonMergeDbToEpm END");
        ////    return Json(new ProxyResponse
        ////    {
        ////        Result = "ok",
        ////    }, JsonRequestBehavior.AllowGet);
        ////}

        [System.Web.Mvc.HttpPost]
        [System.Web.Mvc.AllowAnonymous]
        //button to click on PDP page -> to sync add enteries 
        public async Task<JsonResult> SyncAll(JiraProxyRequest jiraRequest)
        {
            try
            {
                if (jiraRequest == null)
                {
                    return Json(new ProxyResponse
                    {
                        Result = "ko",
                        Data = "Jira request is null"
                    }, JsonRequestBehavior.AllowGet);
                }
                ExecuteJira jiraToDbSyncAll = new ExecuteJira(UnitOfWork);
                await jiraToDbSyncAll.Execute(jiraRequest.ProjectUid, jiraRequest.SystemId, jiraRequest.ProjectId,
                    jiraRequest.EpicKey);
                return Json(new ProxyResponse
                {
                    Result = "ok",
                    Data = jiraRequest.ProjectUid.ToString()
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception exception)
            {
                return HandleException(exception);
            }
        }

        [System.Web.Mvc.HttpPost]
        public async Task<JsonResult> ExecuteRequest([FromBody] JiraProxyRequest jiraRequest)
        {
            if (jiraRequest.SystemId == null)
            {
                return Json(new ProxyResponse
                {
                    Result = "ko",
                    Data = "System Id is null"
                }, JsonRequestBehavior.AllowGet);
            }
            try
            {
                string response = null;
                SyncSystemDTO syncSystem = await SyncSystemBusinessService.GetSyncSystemAsync(jiraRequest.SystemId.Value);
                if (syncSystem != null)
                {
                    JiraAccessService jiraAccessService = new JiraAccessService(syncSystem);
                    response = await jiraAccessService.GetJiraResponse(jiraRequest);
                }
                if (String.IsNullOrEmpty(response))
                {
                    return Json(new ProxyResponse
                    {
                        Result = "ko",
                        Data = "Jira Response is empty"
                    }, JsonRequestBehavior.AllowGet);
                }
                JsonResult jsonResult = Json(new ProxyResponse
                {
                    Result = "ok",
                    Data = response
                }, JsonRequestBehavior.AllowGet);
                jsonResult.MaxJsonLength = Int32.MaxValue;
                return jsonResult;
            }
            catch (Exception exception)
            {
                return HandleException(exception);
            }
        }

        [System.Web.Mvc.HttpPost]
        [System.Web.Mvc.AllowAnonymous]
        public async Task<JsonResult> WebHookReceiver(HttpRequestMessage request, int systemId)
        {
            bool isWebhooksEnabled = Boolean.Parse(ConfigurationManager.AppSettings["IsWebhooksEnabled"]);
            if (!isWebhooksEnabled)
            {
                return Json(new ProxyResponse
                {
                    Result = "ok",
                    Data = "WebHookReceiver POST"
                }, JsonRequestBehavior.AllowGet);
            }

            Logger.Info("WebHookReceiver STARTED SystemId: " + systemId);

            Request.InputStream.Position = 0;
            var streamReader = new StreamReader(Request.InputStream);
            string result = await streamReader.ReadToEndAsync();

            WebHookReceiverJira webHookReceiver = new WebHookReceiverJira(SyncSystemBusinessService,
                MasterBusinessService, CommonBusinessService, SyncSystemFieldMappingBusinessService,
                MasterWorklogBusinessService, Logger);
            await webHookReceiver.AddWebhookToDataBase(result, systemId);
            Logger.Info("WebHookReceiver END");

            return Json(new ProxyResponse
            {
                Result = "ok",
                Data = "WebHookReceiver POST"
            }, JsonRequestBehavior.AllowGet);
        }

        ////probably deprecated, use Common/AssignAllResources instead
        //public async Task<JsonResult> UpdateAssignments(Guid projectUid)
        //{
        //    bool isOnline = Boolean.Parse(ConfigurationManager.AppSettings["IsProjectOnline"]);
        //    int publishAssignmentsMax = int.Parse(ConfigurationManager.AppSettings["PublishAssignmentsMax"]);

        //    DataSyncCommon dataSync = new DataSyncCommon(ConfigurationManager.AppSettings["ProjectOnlineUrl"],
        //        ConfigurationManager.AppSettings["ProjectOnlineUserName"],
        //        ConfigurationManager.AppSettings["ProjectOnlinePassword"], isOnline, publishAssignmentsMax);
        //    dataSync.UpdateAssignments(projectUid);
        //    await Task.Run(() => { });
        //    return Json(new ProxyResponse
        //    {
        //        Result = "ok",
        //    }, JsonRequestBehavior.AllowGet);
        //}
    }
}