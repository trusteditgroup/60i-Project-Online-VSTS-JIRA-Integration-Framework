using System;
using System.Configuration;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Mvc;
using NLog;
using ProjectOnlineSystemConnector.DataModel.Common;
using ProjectOnlineSystemConnector.SyncServices;

namespace ProjectOnlineSystemConnector.Web.Controllers
{
    public class TfsController : BaseController
    {

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        
        public JsonResult GetTest()
        {
            logger.Info("GetTest STARTED");
            return Json(new ProxyResponse
            {
                Result = "ok",
                Data = "GetTest FINISH"
            }, JsonRequestBehavior.AllowGet);
            
        }

        
        public async Task<JsonResult> GetTestSyncAll(int id)
        {
            logger.Info("GetTest STARTED");
            ExecuteTfs syncAll = new ExecuteTfs(UnitOfWork);
            var res =  await syncAll.Execute(2, id, "60i");
            return Json(res, JsonRequestBehavior.AllowGet);

        }

        //private async Task FillWebHookEntryAsync(UnitOfWork unitOfWork, string result)
        //{
        //    logger.Info("WebHookReceiver WebHookEntry");

        //    var webHookEntry = new WebHookEntry
        //    {
        //        JsonRequest = result,
        //        DateCreated = DateTime.Now
        //    };
        //    unitOfWork.WebHookEntryRepository.Add(webHookEntry);
        //    await unitOfWork.SaveChangesAsync();
        //}

        [HttpPost]
        [AllowAnonymous]
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
            logger.Info("WebHookReceiver STARTED SystemId: " + systemId);

            //string result = await request.Content.ReadAsStringAsync();

            Request.InputStream.Position = 0;
            var streamReader = new StreamReader(Request.InputStream);
            string result = await streamReader.ReadToEndAsync();

            WebHookReceiverTfs webHookReceiver = new WebHookReceiverTfs(SyncSystemBusinessService,
                MasterBusinessService, CommonBusinessService, SyncSystemFieldMappingBusinessService, Logger);

            await webHookReceiver.AddWebhookToDataBase(result, systemId);

            Logger.Info("WebHookReceiver END");

            return Json(new ProxyResponse
            {
                Result = "ok",
                Data = "WebHookReceiver POST"
            }, JsonRequestBehavior.AllowGet);


        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<JsonResult> Execute(JiraProxyRequest jiraRequest)
        {
            logger.Info("TFS SyncAll STARTED");

            ExecuteTfs executer = new ExecuteTfs(UnitOfWork);
            int issueId = 0;
            if (jiraRequest.EpicKey != null)
            {
                issueId = int.Parse(jiraRequest.EpicKey);
            }
            string projectId = "";
            if (jiraRequest.ProjectId != null)
            {
                projectId = jiraRequest.ProjectId;
            }
            var executeResult = await executer.Execute((int)jiraRequest.SystemId, issueId, projectId);
            return Json(executeResult, JsonRequestBehavior.AllowGet);

        }

        //private void FillStagingWorklog(UnitOfWork unitOfWork, Staging staging,
        //    WorkItem workItem, int systemId)
        //{
        //    StagingWorklog stagingWorklog;
        //    if (webHookCreateRequest != null)
        //    {
        //        if (webHookCreateRequest.Resource.Fields.ContainsKey(FieldActualHours) &&
        //            webHookCreateRequest.Resource.Fields.ContainsKey(FieldAssignedTo))
        //        {
        //            stagingWorklog = unitOfWork.StagingWorklogRepository.GetWorkLogById(systemId,
        //                webHookCreateRequest.Resource.Id.ToString(), DateTime.Now);
        //            if (stagingWorklog == null)
        //            {
        //                stagingWorklog = new StagingWorklog
        //                {
        //                    DateCreated = DateTime.Now
        //                };
        //                unitOfWork.StagingWorklogRepository.Add(stagingWorklog);
        //            }
        //            stagingWorklog.DateUpdated = DateTime.Now;
        //            stagingWorklog.Staging = staging;
        //            stagingWorklog.RecordState = StateConst.New;
        //            stagingWorklog.DateStarted = DateTime.Now;

        //            double resultWork = double.Parse(webHookCreateRequest.Resource.Fields[FieldActualHours].ToString());
        //            string userEmail = webHookCreateRequest.Resource.Fields[FieldAssignedTo].ToString()
        //                .Substring(webHookCreateRequest.Resource.Fields[FieldAssignedTo].ToString()
        //                    .IndexOf("<", StringComparison.Ordinal))
        //                .Trim('<', '>');
        //            stagingWorklog.IssueId = webHookCreateRequest.Resource.Id.ToString();
        //            stagingWorklog.AuthorName = userEmail;
        //            stagingWorklog.AuthorEmailAddress = userEmail;
        //            stagingWorklog.AuthorKey = userEmail;
        //            stagingWorklog.WorkLogId = webHookCreateRequest.Resource.Id.ToString();
        //            stagingWorklog.TimeSpentSeconds = (long)resultWork * 60;
        //        }
        //    }
        //    if (webHookUpdateRequest != null)
        //    {
        //        if (webHookUpdateRequest.Resource.Fields.ContainsKey(FieldActualHours))
        //        {
        //            stagingWorklog = unitOfWork.StagingWorklogRepository.GetWorkLogById(systemId,
        //                webHookUpdateRequest.Resource.Id.ToString(), DateTime.Now);
        //            if (stagingWorklog == null)
        //            {
        //                stagingWorklog = new StagingWorklog
        //                {
        //                    DateCreated = DateTime.Now
        //                };
        //                unitOfWork.StagingWorklogRepository.Add(stagingWorklog);
        //            }
        //            stagingWorklog.DateUpdated = DateTime.Now;
        //            stagingWorklog.Staging = staging;
        //            stagingWorklog.RecordState = StateConst.New;
        //            stagingWorklog.DateStarted = DateTime.Now;

        //            double newValue, oldValue;
        //            double.TryParse(webHookUpdateRequest.Resource.Fields[FieldActualHours].NewValue, out newValue);
        //            double.TryParse(webHookUpdateRequest.Resource.Fields[FieldActualHours].OldValue, out oldValue);

        //            double resultWork = newValue - oldValue;

        //            string userEmail;

        //            if (webHookUpdateRequest.Resource.Fields.ContainsKey(FieldAssignedTo))
        //            {
        //                userEmail = webHookUpdateRequest.Resource.Fields[FieldAssignedTo].NewValue
        //                    .Substring(webHookUpdateRequest.Resource.Fields[FieldAssignedTo].NewValue.IndexOf("<",
        //                        StringComparison.Ordinal))
        //                    .Trim('<', '>');
        //            }
        //            else
        //            {
        //                userEmail = webHookUpdateRequest.Resource.Revision.Fields[FieldAssignedTo]
        //                    .Substring(webHookUpdateRequest.Resource.Revision.Fields[FieldAssignedTo].IndexOf("<",
        //                        StringComparison.Ordinal))
        //                    .Trim('<', '>');
        //            }
        //            stagingWorklog.IssueId = webHookUpdateRequest.Resource.Id.ToString();
        //            stagingWorklog.AuthorName = userEmail;
        //            stagingWorklog.AuthorEmailAddress = userEmail;
        //            stagingWorklog.AuthorKey = userEmail;
        //            stagingWorklog.WorkLogId = webHookUpdateRequest.Resource.Id.ToString();
        //            stagingWorklog.TimeSpentSeconds = (long)resultWork * 60;
        //        }
        //    }
        //}

        //private void FillTimeSheet(double resultWork, string userEmail)
        //{
        //    if (!usersPasswords.ContainsKey(userEmail))
        //    {
        //        logger.Warn("Unknown email");
        //        return;
        //    }

        //    var secureString = new SecureString();
        //    foreach (char c in usersPasswords[userEmail])
        //    {
        //        secureString.AppendChar(c);
        //    }
        //    projContext = new ProjectContext(PwaPath)
        //    {
        //        Credentials = new SharePointOnlineCredentials(userEmail, secureString)
        //    };

        //    Web web = projContext.Web;
        //    RegionalSettings regSettings = web.RegionalSettings;
        //    projContext.Load(web);
        //    projContext.Load(regSettings); //To get regional settings properties  
        //    Microsoft.SharePoint.Client.TimeZone projectOnlineTimeZone = regSettings.TimeZone;
        //    projContext.Load(projectOnlineTimeZone);  //To get the TimeZone propeties for the current web region settings  
        //    projContext.ExecuteQuery();

        //    TimeSpan projectOnlineUtcOffset = TimeSpan.Parse(projectOnlineTimeZone.Description.Substring(4, projectOnlineTimeZone.Description.IndexOf(")", StringComparison.Ordinal) - 4));
        //    ReadOnlyCollection<TimeZoneInfo> timeZones = TimeZoneInfo.GetSystemTimeZones();
        //    TimeZoneInfo projectOnlineTimeZoneInfo = timeZones.FirstOrDefault(x => x.BaseUtcOffset == projectOnlineUtcOffset);
        //    DateTime currentDateForCompare = DateTime.Now;
        //    if (projectOnlineTimeZoneInfo != null)
        //    {
        //        currentDateForCompare = TimeZoneInfo.ConvertTime(DateTime.Now, projectOnlineTimeZoneInfo);
        //    }

        //    projContext.Load(projContext.TimeSheetPeriods, c => c.Where(p => p.Start <= currentDateForCompare && p.End >= currentDateForCompare)
        //            .IncludeWithDefaultProperties(p => p.TimeSheet, p => p.Id, p => p.TimeSheet.Lines
        //                  .IncludeWithDefaultProperties(l => l.Assignment, l => l.Id, l => l.Assignment.Task, l => l.Work)));
        //    projContext.ExecuteQuery();
        //    TimeSheetPeriod maPeriod = projContext.TimeSheetPeriods.FirstOrDefault();

        //    if (maPeriod == null || maPeriod.TimeSheet.Status == TimeSheetStatus.Approved ||
        //        maPeriod.TimeSheet.Status == TimeSheetStatus.Submitted ||
        //        maPeriod.TimeSheet.Status == TimeSheetStatus.Rejected)
        //    {
        //        return;
        //    }

        //    TimeSheetLine line = maPeriod.TimeSheet.Lines.FirstOrDefault(l => l.ProjectName == ProjectName);
        //    if (line == null)
        //    {
        //        TimeSheetLineCreationInformation lineCreation = new TimeSheetLineCreationInformation
        //        {
        //            ProjectId = Guid.Parse(ProjectUid),
        //            TaskName = "Line Creation test",
        //            Id = Guid.NewGuid()
        //        };

        //        maPeriod.TimeSheet.Lines.Add(lineCreation);
        //        maPeriod.TimeSheet.Update();
        //        projContext.ExecuteQuery();

        //        projContext.Load(projContext.TimeSheetPeriods, c => c.Where(p => p.Start <= currentDateForCompare && p.End >= currentDateForCompare)
        //            .IncludeWithDefaultProperties(p => p.TimeSheet, p => p.Id, p => p.TimeSheet.Lines
        //                  .IncludeWithDefaultProperties(l => l.Assignment, l => l.Id, l => l.Assignment.Task, l => l.Work)));
        //        projContext.ExecuteQuery();
        //        maPeriod = projContext.TimeSheetPeriods.FirstOrDefault();
        //        line = maPeriod.TimeSheet.Lines.FirstOrDefault(l => l.ProjectName == ProjectName);
        //    }
        //    TimeSheetWork work = line.Work.FirstOrDefault(w => w.Start.Date == currentDateForCompare.Date && w.End.Date == currentDateForCompare.Date);
        //    if (work == null)
        //    {
        //        TimeSheetWorkCreationInformation workCreation = new TimeSheetWorkCreationInformation
        //        {
        //            ActualWork = string.Format(CultureInfo.GetCultureInfo("en-US"), "{0}h", resultWork),
        //            Start = DateTime.Now,
        //            End = DateTime.Now,
        //            Comment = "Work Creation Test",
        //            NonBillableOvertimeWork = "0",
        //            NonBillableWork = "0",
        //            OvertimeWork = "0",
        //            PlannedWork = "0"
        //        };

        //        line.Work.Add(workCreation);
        //        maPeriod.TimeSheet.Update();
        //        projContext.ExecuteQuery();
        //    }
        //    else
        //    {
        //        double actualWork = Double.Parse(work.ActualWork.Trim('h'));
        //        actualWork += resultWork;
        //        work.ActualWork = string.Format(CultureInfo.GetCultureInfo("en-US"), "{0}h", actualWork);
        //        maPeriod.TimeSheet.Update();
        //        projContext.ExecuteQuery();
        //    }
        //}

        //[HttpGet]
        //public HttpResponseMessage RefreshCache()
        //{
        //    return Request.CreateResponse(HttpStatusCode.OK, new TfsResponse { Data = "OK" });
        //}
    }

}