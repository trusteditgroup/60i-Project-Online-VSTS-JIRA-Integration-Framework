#region License

/* 
 * Copyright © 2017 Yaroslav Boychenko.
 * Copyright © 2017 TrustedIt Group. Contacts: mailto:60i@trusteditgroup.com
 * This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by Trusted IT Group Inc.
 * This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 * See the GNU General Public License for more details. You should have received a copy of the GNU General Public License along with this program.
 * If not, see https://www.trusteditgroup.com/60ilicense
 */

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;
using NLog;
using ProjectOnlineSystemConnector.BusinessServices;
using ProjectOnlineSystemConnector.Common;
using ProjectOnlineSystemConnector.Data.Views.EntityClasses;
using ProjectOnlineSystemConnector.DataModel.DTO;
using ProjectOnlineSystemConnector.DataModel.Common;
using ProjectOnlineSystemConnector.DataModel.TfsWebHook;
using ProjectOnlineSystemConnector.DataModel.ViewModel;
using WIT = Microsoft.TeamFoundation.WorkItemTracking.WebApi;

namespace ProjectOnlineSystemConnector.SyncServices
{
    public class WebHookReceiverTfs
    {
        private const string FieldAssignedTo = "System.AssignedTo";
        private const string FieldEventType = "eventType";

        private readonly Logger logger;
        private readonly SyncSystemBusinessService syncSystemBusinessService;
        private readonly MasterBusinessService masterBusinessService;
        private readonly CommonBusinessService commonBusinessService;
        private readonly SyncSystemFieldMappingBusinessService syncSystemFieldMappingBusinessService;

        public WebHookReceiverTfs(SyncSystemBusinessService syncSystemBusinessService,
            MasterBusinessService masterBusinessService, CommonBusinessService commonBusinessService,
            SyncSystemFieldMappingBusinessService syncSystemFieldMappingBusinessService, Logger logger)
        {
            this.logger = logger;
            this.syncSystemBusinessService = syncSystemBusinessService;
            this.masterBusinessService = masterBusinessService;
            this.commonBusinessService = commonBusinessService;
            this.syncSystemFieldMappingBusinessService = syncSystemFieldMappingBusinessService;
        }

        public async Task<ProxyResponse> AddWebhookToDataBase(string jsonWebhook, int systemId)
        {
            Dictionary<string, object> commonDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonWebhook);
            SystemToDbViewModel systemToDbViewModel = new SystemToDbViewModel();

            logger.Info("WebHookReceiver UnitOfWork");
            int? id = 0;
            switch (commonDictionary[FieldEventType].ToString())
            {
                case "workitem.created":
                    var webHookCreateRequest = JsonConvert.DeserializeObject<WebHookCreateRequest>(jsonWebhook);
                    id = webHookCreateRequest.Resource?.Id;
                    break;
                case "workitem.updated":
                    var webHookUpdateRequest = JsonConvert.DeserializeObject<WebHookUpdateRequest>(jsonWebhook);
                    id = webHookUpdateRequest.Resource?.WorkItemId;
                    break;
            }
            SyncSystemDTO syncSystem = await syncSystemBusinessService.GetSyncSystemAsync(systemId);

            if (syncSystem == null || !id.HasValue || id == 0)
            {
                //await FillWebHookEntryAsync(unitOfWork, result);
                logger.Info("System not found " + systemId);
                logger.Info("WebHookReceiver END");
                return new ProxyResponse
                {
                    Result = "ok",
                    Data = "WebHookReceiver POST"
                };
            }

            List<SyncSystemFieldMapping> syncSystemFieldMappings = syncSystemFieldMappingBusinessService.GetSyncSystemFieldMappings(systemId);

            //string.Empty, syncSystem.SystemPassword
            //VssConnection connection = new VssConnection(new Uri(syncSystem.SystemUrl + "/DefaultCollection"), new VssCredentials());

            VssConnection connection = new VssConnection(new Uri(syncSystem.SystemUrl + "/DefaultCollection"),
                new VssBasicCredential(String.Empty, syncSystem.SystemPassword));
            // Create instance of WorkItemTrackingHttpClient using VssConnection
            WIT.WorkItemTrackingHttpClient witClient = connection.GetClient<WIT.WorkItemTrackingHttpClient>();
            WorkItem workItem = await witClient.GetWorkItemAsync(id.Value, expand: WorkItemExpand.Relations);

            //DbContextTransaction transaction = unitOfWork.BeginTransaction();
            try
            {
                #region Staging

                var staging = new Staging
                {
                    RecordDateCreated = DateTime.Now,
                    SystemId = systemId,
                    ChangedFields = "all",
                    RecordDateUpdated = DateTime.Now,
                    RecordState = RecordStateConst.New,
                    WebHookEvent = commonDictionary[FieldEventType].ToString()
                };

                if (workItem.Fields.ContainsKey("System.TeamProject"))
                {
                    staging.ProjectId = (string)workItem.Fields["System.TeamProject"];
                    staging.ProjectKey = (string)workItem.Fields["System.TeamProject"];
                    staging.ProjectName = (string)workItem.Fields["System.TeamProject"];
                }
                staging.IssueId = workItem.Id?.ToString();
                staging.IssueKey = workItem.Id?.ToString();
                if (workItem.Fields.ContainsKey("System.WorkItemType"))
                {
                    staging.IssueTypeId = workItem.Fields["System.WorkItemType"].ToString();
                    staging.IssueTypeName = workItem.Fields["System.WorkItemType"].ToString();
                }
                if (workItem.Fields.ContainsKey("System.Title"))
                {
                    staging.IssueName = workItem.Fields["System.Title"].ToString();
                }
                if (workItem.Fields.ContainsKey("System.IterationId"))
                {
                    staging.ParentVersionId = workItem.Fields["System.IterationId"].ToString();
                }
                if (workItem.Fields.ContainsKey("System.IterationLevel2"))
                {
                    staging.ParentVersionName = workItem.Fields["System.IterationLevel2"].ToString();
                }
                if (workItem.Fields.ContainsKey(FieldAssignedTo))
                {
                    if (workItem.Fields[FieldAssignedTo] != null)
                    {
                        staging.Assignee = workItem.Fields[FieldAssignedTo].ToString()
                            .Substring(workItem.Fields[FieldAssignedTo].ToString()
                            .IndexOf("<", StringComparison.Ordinal)).Trim('<', '>');
                    }
                }
                if (workItem.Relations != null)
                {
                    WorkItemRelation relationCrt = workItem.Relations
                        .FirstOrDefault(x => x.Rel == "System.LinkTypes.Hierarchy-Reverse");
                    if (relationCrt != null)
                    {
                        if (workItem.Fields["System.WorkItemType"].ToString().ToLower().Equals("feature"))
                        {
                            staging.ParentEpicId = relationCrt.Url.Substring(relationCrt.Url.LastIndexOf("/", StringComparison.Ordinal) + 1);
                            staging.ParentEpicKey = relationCrt.Url.Substring(relationCrt.Url.LastIndexOf("/", StringComparison.Ordinal) + 1);
                        }
                        else
                        {
                            staging.ParentIssueId = relationCrt.Url.Substring(relationCrt.Url.LastIndexOf("/", StringComparison.Ordinal) + 1);
                            staging.ParentIssueKey = relationCrt.Url.Substring(relationCrt.Url.LastIndexOf("/", StringComparison.Ordinal) + 1);
                        }

                    }
                    else
                    {
                        staging.ParentIssueId = null;
                        staging.ParentIssueKey = null;
                    }
                }
                systemToDbViewModel.StagingsToInsert.Add(staging);
                //FillStagingWorklog(unitOfWork, staging, workItem, systemId);

                SyncSystemFieldMapping fieldMappingRecordStateGeneral = syncSystemFieldMappings
                    .FirstOrDefault(x => x.SystemId == staging.SystemId &&
                                         x.EpmFieldName == ProjectServerConstants.RecordStateGeneral);

                if (fieldMappingRecordStateGeneral != null)
                {
                    var stagingFieldMappingValueGeneral = new StagingFieldMappingValue
                    {
                        Staging = staging,
                        SyncSystemFieldMappingId = fieldMappingRecordStateGeneral.SyncSystemFieldMappingId,
                        Value = RecordStateConst.New
                    };
                    systemToDbViewModel.StagingFieldMappingValuesToInsert.Add(stagingFieldMappingValueGeneral);
                }

                SyncSystemFieldMapping fieldMappingRecordStateActual = syncSystemFieldMappings
                    .FirstOrDefault(x => x.SystemId == staging.SystemId &&
                                         x.EpmFieldName == ProjectServerConstants.RecordStateActual);

                if (fieldMappingRecordStateActual != null)
                {
                    var stagingFieldMappingValueActual = new StagingFieldMappingValue
                    {
                        Staging = staging,
                        SyncSystemFieldMappingId = fieldMappingRecordStateActual.SyncSystemFieldMappingId,
                        Value = RecordStateConst.Done
                    };
                    systemToDbViewModel.StagingFieldMappingValuesToInsert.Add(stagingFieldMappingValueActual);
                }

                #endregion

                #region Master

                var master = masterBusinessService.GetIssueById(systemId, id.ToString(), staging.IssueTypeId);
                if (master == null)
                {
                    master = new Master
                    {
                        RecordDateCreated = DateTime.Now
                    };
                    systemToDbViewModel.MastersToInsert.Add(master);
                }

                master.RecordDateUpdated = DateTime.Now;
                master.SystemId = systemId;

                master.IssueId = staging.IssueId;
                master.IssueKey = staging.IssueKey;
                master.IssueName = staging.IssueName;

                master.ProjectId = staging.ProjectId;
                master.ProjectKey = staging.ProjectKey;
                master.ProjectName = staging.ProjectName;

                master.IsSubTask = staging.IsSubTask;

                master.ParentVersionReleased = staging.ParentVersionReleased;
                master.IssueTypeName = staging.IssueTypeName;
                master.IssueTypeId = staging.IssueTypeId;
                master.DateFinish = staging.DateFinish;
                master.DateRelease = staging.DateRelease;

                #endregion

                #region MasterHistory

                var masterHistory = new MasterHistory
                {
                    RecordDateCreated = DateTime.Now
                };
                systemToDbViewModel.MasterHistoriesToInsert.Add(masterHistory);

                masterHistory.RecordDateUpdated = DateTime.Now;
                masterHistory.SystemId = systemId;

                masterHistory.IssueId = staging.IssueId;
                masterHistory.IssueKey = staging.IssueKey;
                masterHistory.IssueName = staging.IssueName;

                masterHistory.ProjectId = staging.ProjectId;
                masterHistory.ProjectKey = staging.ProjectKey;
                masterHistory.ProjectName = staging.ProjectName;

                masterHistory.IsSubTask = staging.IsSubTask;

                masterHistory.ParentVersionReleased = staging.ParentVersionReleased;
                masterHistory.IssueTypeName = staging.IssueTypeName;
                masterHistory.IssueTypeId = staging.IssueTypeId;
                masterHistory.DateFinish = staging.DateFinish;
                masterHistory.DateRelease = staging.DateRelease;


                #endregion

                await commonBusinessService.AddNewData(systemToDbViewModel);
                //await unitOfWork.SaveChangesAsync();
                //unitOfWork.CommitTransaction(transaction);
            }
            //catch (DbEntityValidationException exception)
            //{
            //    logger.Fatal(exception);
            //    unitOfWork.RollbackTransaction(transaction);

            //    foreach (DbEntityValidationResult validationResult in exception.EntityValidationErrors)
            //    {
            //        foreach (DbValidationError error in validationResult.ValidationErrors)
            //        {
            //            logger.Fatal(error.PropertyName + " " + error.ErrorMessage);
            //        }
            //    }
            //}
            catch (Exception exception)
            {
                logger.Fatal(exception);
                //unitOfWork.RollbackTransaction(transaction);
            }

            logger.Info("WebHookReceiver END");
            return new ProxyResponse
            {
                Result = "ok",
                Data = "WebHookReceiver POST"
            };
        }
    }

}