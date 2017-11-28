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
using System.Reflection;
using System.Threading.Tasks;
using Atlassian.Jira;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using ProjectOnlineSystemConnector.BusinessServices;
using ProjectOnlineSystemConnector.Common;
using ProjectOnlineSystemConnector.Data.Views.EntityClasses;
using ProjectOnlineSystemConnector.DataModel.DTO;
using ProjectOnlineSystemConnector.DataAccess.Jira;
using ProjectOnlineSystemConnector.DataModel.Common;
using ProjectOnlineSystemConnector.DataModel.JiraWebHook;
using ProjectOnlineSystemConnector.DataModel.ViewModel;

namespace ProjectOnlineSystemConnector.SyncServices
{
    public class WebHookReceiverJira
    {
        private CustomField jiraEpicLinkField;
        private List<CustomField> customFields;

        private readonly Logger logger;
        private readonly SyncSystemBusinessService syncSystemBusinessService;
        private readonly MasterBusinessService masterBusinessService;
        private readonly MasterWorklogBusinessService masterWorklogBusinessService;
        private readonly CommonBusinessService commonBusinessService;
        private readonly SyncSystemFieldMappingBusinessService syncSystemFieldMappingBusinessService;

        public WebHookReceiverJira(SyncSystemBusinessService syncSystemBusinessService,
            MasterBusinessService masterBusinessService, CommonBusinessService commonBusinessService,
            SyncSystemFieldMappingBusinessService syncSystemFieldMappingBusinessService, MasterWorklogBusinessService masterWorklogBusinessService, Logger logger)
        {
            this.logger = logger;
            this.syncSystemBusinessService = syncSystemBusinessService;
            this.masterBusinessService = masterBusinessService;
            this.commonBusinessService = commonBusinessService;
            this.syncSystemFieldMappingBusinessService = syncSystemFieldMappingBusinessService;
            this.masterWorklogBusinessService = masterWorklogBusinessService;
        }

        public async Task<ProxyResponse> AddWebhookToDataBase(string jsonWebhook, int systemId)
        {
            logger.Info("WebHookReceiver UnitOfWork");

            JiraAccessService jiraAccessService = null;
            SyncSystemDTO syncSystem = await syncSystemBusinessService.GetSyncSystemAsync(systemId);

            if (syncSystem != null)
            {
                jiraAccessService = new JiraAccessService(syncSystem);
            }
            if (jiraAccessService == null)
            {
                //await FillWebHookEntryAsync(unitOfWork, jsonWebhook);
                logger.Error("System not found " + systemId);
                logger.Info("WebHookReceiver END");
                return new ProxyResponse
                {
                    Result = "ok",
                    Data = "WebHookReceiver POST"
                };
            }
            List<SyncSystemFieldMapping> syncSystemFieldMappings = syncSystemFieldMappingBusinessService.GetSyncSystemFieldMappings(syncSystem.SystemId);

            logger.Info("WebHookReceiver Get Fields Start");
            customFields = await jiraAccessService.GetCustomFields();
            if (customFields == null)
            {
                logger.Error("WebHookReceiver Custom fields for system " + systemId + " is NULL!");
                logger.Info("WebHookReceiver END");
                return new ProxyResponse
                {
                    Result = "ok",
                    Data = "WebHookReceiver POST"
                };
            }
            jiraEpicLinkField = customFields.FirstOrDefault(x => x.Name == JiraConstants.EpicLinkFieldName);

            logger.Info("WebHookReceiver Get Fields End");
            JiraRequest jiraRequest = null;
            string jiraEpicLink = null;
            string jiraEpicName = null;
            try
            {
                jiraRequest = JsonConvert.DeserializeObject<JiraRequest>(jsonWebhook);
                if (jiraRequest == null)
                {
                    //await FillWebHookEntryAsync(unitOfWork, jsonWebhook);
                    logger.Info("WebHookReceiver END");
                    return new ProxyResponse
                    {
                        Result = "ok",
                        Data = "WebHookReceiver POST"
                    };
                }
                logger.Info($"WebHookReceiver syncSystem.SystemName: {syncSystem.SystemName}; " +
                            $"jiraRequest.WebhookEvent: {jiraRequest.WebhookEvent}");

                if (jiraRequest.Issue != null)
                {
                    jiraRequest.Issue.AllFields = GetIssueFieldsDictionary(jsonWebhook);

                    if (jiraRequest.Issue.Fields.IssueType.Name != "Epic")
                    {
                        if (jiraEpicLinkField != null)
                        {
                            jiraEpicLink = (string)jiraRequest.Issue.GetField(jiraEpicLinkField.Id);
                            Master masterEpic = masterBusinessService.GetMaster(syncSystem.SystemId, jiraEpicLink);
                            if (masterEpic != null)
                            {
                                jiraEpicName = masterEpic.IssueName;
                            }
                        }
                    }
                    else
                    {
                        jiraEpicName = jiraRequest.Issue.Fields.Summary.Trim();
                    }

                    logger.Info(syncSystem.SystemName + ": " + jiraRequest.WebhookEvent
                                + "; ProjectId: " + jiraRequest.Issue.Fields.Project.Id
                                + "; ProjectKey: " + jiraRequest.Issue.Fields.Project.Key
                                + "; IssueKey: " + jiraRequest.Issue.Key
                                + "; IssueName: " + jiraRequest.Issue.Fields.Summary.Trim()
                                + "; ChangedFields: " + GetChangedFields(jiraRequest.ChangeLog?.Items));
                }
            }
            catch (Exception exception)
            {
                HandleException(exception);
            }
            jiraEpicName = jiraEpicName.RemoveNewLinesTabs().Truncate();
            //DbContextTransaction transaction = unitOfWork.BeginTransaction();
            logger.Info("WebHookReceiver DbContextTransaction");
            try
            {
                SystemToDbViewModel systemToDbViewModel = new SystemToDbViewModel();
                Staging staging;
                MasterHistory masterHistory;
                Master master;
                if (jiraRequest?.Issue != null)
                {
                    if (jiraRequest.WebhookEvent == "jira:issue_deleted")
                    {
                        logger.Info("WebHookReceiver END");

                        return new ProxyResponse
                        {
                            Result = "ok",
                            Data = "WebHookReceiver POST"
                        };
                    }
                    master = FillMasterIssueAsync(systemToDbViewModel, jiraRequest, jiraEpicLink, syncSystem);
                    masterHistory = FillMasterHistoryIssueAsync(systemToDbViewModel, jiraRequest, jiraEpicLink, syncSystem);
                    staging = FillStagingIssueAsync(systemToDbViewModel, jiraRequest, jiraEpicLink, syncSystem,
                        syncSystemFieldMappings);
                    FillCustomFields(systemToDbViewModel, jiraRequest, master, staging, masterHistory,
                        syncSystemFieldMappings, jiraEpicName);

                    AddLastUpdateUser(systemToDbViewModel, jiraRequest, staging, syncSystemFieldMappings);
                    AddSprints(systemToDbViewModel, jiraRequest, jiraAccessService, master, staging, masterHistory,
                        syncSystemFieldMappings);
                }
                if (jiraRequest?.Worklog != null)
                {
                    FillWorkLog(systemToDbViewModel, jiraRequest, systemId);
                }
                if (jiraRequest?.Version != null)
                {
                    master = FillMasterVersionAsync(systemToDbViewModel, jiraRequest, systemId);
                    masterHistory = FillMasterHistoryVersionAsync(systemToDbViewModel, jiraRequest, systemId);
                    staging = FillStagingVersionAsync(systemToDbViewModel, jiraRequest, systemId, syncSystemFieldMappings);

                    if (staging != null)
                    {
                        SyncSystemFieldMapping fieldMappingRecordStateGeneral = syncSystemFieldMappings
                            .FirstOrDefault(x => x.SystemId == staging.SystemId &&
                                                 x.EpmFieldName == ProjectServerConstants.RecordStateGeneral);
                        SetCustomFields(systemToDbViewModel, fieldMappingRecordStateGeneral, master, staging, masterHistory,
                            RecordStateConst.New, null, true);

                        SyncSystemFieldMapping fieldMappingRecordStateActual = syncSystemFieldMappings
                            .FirstOrDefault(x => x.SystemId == staging.SystemId &&
                                                 x.EpmFieldName == ProjectServerConstants.RecordStateActual);
                        SetCustomFields(systemToDbViewModel, fieldMappingRecordStateActual, master, staging, masterHistory,
                            RecordStateConst.Done, null, true);
                    }
                }
                await commonBusinessService.AddNewData(systemToDbViewModel);
            }
            catch (Exception exception)
            {
                logger.Error(exception);
            }
            //catch (DbUpdateException exception)
            //{
            //    foreach (DbEntityEntry dbEntityEntry in exception.Entries)
            //    {
            //        string resultString = dbEntityEntry.CurrentValues.PropertyNames.Aggregate("",
            //            (current, propertyName) =>
            //                current +
            //                $"propertyName: {propertyName} - value: {dbEntityEntry.CurrentValues[propertyName]};");
            //        logger.Fatal(
            //            $"WebHookReceiver DbUpdateException dbEntityEntry.Entity {dbEntityEntry.Entity}; resultString: {resultString}");
            //    }
            //}
            //catch (DbEntityValidationException exception)
            //{
            //    HandleException(exception);
            //    unitOfWork.RollbackTransaction(transaction);

            //    foreach (DbEntityValidationResult validationResult in exception.EntityValidationErrors)
            //    {
            //        foreach (DbValidationError error in validationResult.ValidationErrors)
            //        {
            //            HandleException(error.PropertyName + " " + error.ErrorMessage, true);
            //        }
            //    }
            //}
            //catch (Exception exception)
            //{
            //    HandleException(exception);
            //    unitOfWork.RollbackTransaction(transaction);
            //}
            ////finally
            ////{
            ////    await FillWebHookEntryAsync(unitOfWork, jsonWebhook);
            ////}
            return new ProxyResponse
            {
                Result = "ok",
                Data = "WebHookReceiver POST"
            };
        }

        public bool CheckChangeLog(JiraRequest jiraRequest, List<SyncSystemFieldMapping> syncSystemFieldMappings)
        {
            if (jiraRequest.WebhookEvent == "jira:worklog_updated" || jiraRequest.WebhookEvent == "jira:issue_created")
            {
                return true;
            }
            string changedFileds = GetChangedFields(jiraRequest.ChangeLog?.Items);
            if (String.IsNullOrEmpty(changedFileds))
            {
                return false;
            }
            List<string> actualSystemFieldNames = syncSystemFieldMappings
                .Where(x => !String.IsNullOrEmpty(x.SystemFieldName) || x.FieldType == "ChangeTracker").Select(x => x.SystemFieldName)
                .ToList();
            return actualSystemFieldNames.Any(actualSystemFieldName => changedFileds.Contains(actualSystemFieldName));
        }

        //on jira request we want to get Sprints to our DB -> we got 
        //customfield in webhook for it, so we just need to parse it 
        //into our values, and as we don't have rows for them in our tables
        //we store them in field mapping tables
        private void AddSprints(SystemToDbViewModel systemToDbViewModel, JiraRequest jiraRequest, JiraAccessService jiraAccessService,
            Master master, Staging staging, MasterHistory masterHistory, List<SyncSystemFieldMapping> syncSystemFieldMappings)
        {
            //we receiving fild from jira and getting its value 
            CustomField customFieldSprint = customFields.FirstOrDefault(x => x.Name == JiraConstants.SprintFieldName);
            logger.Info($"WebHookReceiver AddSprints issue.Key: {jiraRequest.Issue.Key}");
            if (customFieldSprint != null && jiraRequest.Issue.GetField(customFieldSprint.Id) != null)
            {
                string[] sprintsStrings = JsonConvert.DeserializeObject<string[]>(jiraRequest.Issue.GetField(customFieldSprint.Id).ToString());

                logger.Info($"WebHookReceiver AddSprints jiraRequest.Issue.Key: {jiraRequest.Issue.Key}; " +
                            $"customFieldSprint.Name: {customFieldSprint.Name}; " +
                            $"customFieldSprint.Id: {customFieldSprint.Id}; " +
                            $"sprints.Length: {sprintsStrings.Length}");

                List<MasterFieldMappingValue> masterFieldMappingValues = new List<MasterFieldMappingValue>();
                List<MasterHistoryFieldMappingValue> masterHistoryFieldMappingValues = new List<MasterHistoryFieldMappingValue>();
                List<StagingFieldMappingValue> stagingFieldMappingValues = new List<StagingFieldMappingValue>();

                JiraSprint jiraSprint = jiraAccessService.ParseSprintField(sprintsStrings);
                commonBusinessService.SetSprintField(jiraSprint, syncSystemFieldMappings, master, masterHistory, staging,
                    masterFieldMappingValues, masterHistoryFieldMappingValues, stagingFieldMappingValues);

                systemToDbViewModel.MasterFieldMappingValuesToInsert.AddRange(masterFieldMappingValues);
                systemToDbViewModel.MasterHistoryFieldMappingValuesToInsert.AddRange(masterHistoryFieldMappingValues);
                systemToDbViewModel.StagingFieldMappingValuesToInsert.AddRange(stagingFieldMappingValues);
            }
        }

        //it also works with mappings
        private void AddLastUpdateUser(SystemToDbViewModel systemToDbViewModel, JiraRequest jiraRequest, Staging staging, List<SyncSystemFieldMapping> syncSystemFieldMappings)
        {
            if (staging == null)
            {
                return;
            }
            if (jiraRequest.User == null)
            {
                return;
            }
            if (jiraRequest.Issue?.Fields?.Assignee != null &&
                jiraRequest.User.Name == jiraRequest.Issue.Fields.Assignee.Name)
            {
                return;
            }
            SyncSystemFieldMapping lastUpdateUserFieldMapping = syncSystemFieldMappings
                .FirstOrDefault(x => x.EpmFieldName == ProjectServerConstants.LastUpdateUser);
            if (lastUpdateUserFieldMapping != null)
            {
                var stagingFieldMappingValue = new StagingFieldMappingValue
                {
                    Staging = staging,
                    SyncSystemFieldMappingId = lastUpdateUserFieldMapping.SyncSystemFieldMappingId,
                    Value = jiraRequest.User.Key
                };
                systemToDbViewModel.StagingFieldMappingValuesToInsert.Add(stagingFieldMappingValue);
            }
        }

        private void SetCustomFields(SystemToDbViewModel systemToDbViewModel, SyncSystemFieldMapping fieldMapping,
            Master master, Staging staging, MasterHistory masterHistory, string resultValue,
            List<MasterFieldMappingValue> masterFieldMappingValues, bool isOnlyStaging = false)
        {
            if (fieldMapping == null)
            {
                return;
            }
            if (!isOnlyStaging)
            {
                if (master != null)
                {
                    MasterFieldMappingValue masterValue = masterFieldMappingValues
                        .FirstOrDefault(x => x.SyncSystemFieldMappingId == fieldMapping.SyncSystemFieldMappingId);
                    if (masterValue == null)
                    {
                        masterValue = new MasterFieldMappingValue
                        {
                            Master = master,
                            SyncSystemFieldMappingId = fieldMapping.SyncSystemFieldMappingId
                        };
                        systemToDbViewModel.MasterFieldMappingValuesToInsert.Add(masterValue);
                    }
                    if (!String.IsNullOrEmpty(resultValue))
                    {
                        masterValue.Value = resultValue.RemoveNewLinesTabs();
                    }
                }
                if (masterHistory != null)
                {
                    MasterHistoryFieldMappingValue masterHistoryValue = new MasterHistoryFieldMappingValue
                    {
                        MasterHistory = masterHistory,
                        SyncSystemFieldMappingId = fieldMapping.SyncSystemFieldMappingId,
                        Value = resultValue
                    };
                    systemToDbViewModel.MasterHistoryFieldMappingValuesToInsert.Add(masterHistoryValue);
                }
            }
            if (staging != null)
            {
                StagingFieldMappingValue stagingValue = new StagingFieldMappingValue
                {
                    Staging = staging,
                    SyncSystemFieldMappingId = fieldMapping.SyncSystemFieldMappingId,
                    Value = resultValue
                };
                systemToDbViewModel.StagingFieldMappingValuesToInsert.Add(stagingValue);
            }
            if (!String.IsNullOrEmpty(fieldMapping.StagingFieldName))
            {
                if (staging != null)
                {
                    PropertyInfo piStaging = typeof(Staging).GetProperty(fieldMapping.StagingFieldName);
                    piStaging?.SetValue(staging, resultValue);
                }

                if (!isOnlyStaging)
                {
                    PropertyInfo piMaster = typeof(Master).GetProperty(fieldMapping.StagingFieldName);
                    piMaster?.SetValue(master, resultValue);

                    PropertyInfo piMasterHistory = typeof(MasterHistory).GetProperty(fieldMapping.StagingFieldName);
                    piMasterHistory?.SetValue(masterHistory, resultValue);
                }
            }
        }

        private void FillCustomFields(SystemToDbViewModel systemToDbViewModel, JiraRequest jiraRequest, Master master,
            Staging staging, MasterHistory masterHistory, List<SyncSystemFieldMapping> fieldMappings,
            string epicName)
        {
            List<MasterFieldMappingValue> masterFieldMappingValues =
                masterBusinessService.GetMasterFieldMappingValues(master.MasterId);

            foreach (SyncSystemFieldMapping fieldMapping in fieldMappings)
            {
                string resultValue = "";
                if (fieldMapping.EpmFieldName == ProjectServerConstants.RecordStateGeneral)
                {
                    SetCustomFields(systemToDbViewModel, fieldMapping, master, staging, masterHistory, RecordStateConst.New, null, true);
                    continue;
                }
                if (fieldMapping.EpmFieldName == ProjectServerConstants.RecordStateActual)
                {
                    SetCustomFields(systemToDbViewModel, fieldMapping, master, staging, masterHistory,
                        RecordStateConst.Done, null, true);
                    continue;
                }
                if (String.IsNullOrEmpty(fieldMapping.SystemFieldName))
                {
                    continue;
                }
                if (fieldMapping.EpmFieldName == ProjectServerConstants.EpicName
                    && !String.IsNullOrEmpty(epicName))
                {
                    SetCustomFields(systemToDbViewModel, fieldMapping, master, staging, masterHistory, epicName, masterFieldMappingValues);
                    continue;
                }
                CustomField customField = customFields
                    .FirstOrDefault(x => x.Name == fieldMapping.SystemFieldName);
                if (customField != null)
                {

                    object customFieldValue = jiraRequest.Issue.GetField(customField.Id);
                    if (customFieldValue != null)
                    {
                        if (fieldMapping.IsMultiSelect && customFieldValue is JArray)
                        {
                            //logger.Info($"FillCustomFields JArray: {customFieldValue}");
                            var array = (JArray)customFieldValue;
                            resultValue = array.Aggregate(resultValue, (current, jToken) => current + (jToken + ",")).Trim(',');
                        }
                        else
                        {
                            if (fieldMapping.IsIdWithValue)
                            {
                                //logger.Info($"WebHookReceiver FillCustomFields fieldMapping.SystemFieldName: {fieldMapping.SystemFieldName} customFieldValue.ToString(): {customFieldValue}");
                                var objectProperties = JsonConvert.DeserializeObject<Dictionary<string, object>>(customFieldValue.ToString());

                                if (objectProperties.ContainsKey("value"))
                                {
                                    resultValue = objectProperties["value"].ToString();
                                }
                            }
                            else
                            {
                                resultValue = customFieldValue.ToString();
                            }
                        }
                        if (String.IsNullOrEmpty(resultValue))
                        {
                            continue;
                        }
                        SetCustomFields(systemToDbViewModel, fieldMapping, master, staging, masterHistory, resultValue, masterFieldMappingValues);
                    }
                }
            }
            //await unitOfWork.SaveChangesAsync();
        }

        //function that directly updates our Master table in DB
        private Master FillMasterIssueAsync(SystemToDbViewModel systemToDbViewModel, JiraRequest jiraRequest, string jiraEpicLink, SyncSystemDTO syncSystem)
        {
            logger.Info("WebHookReceiver Issue Master");
            Master master = masterBusinessService.GetIssueById(syncSystem.SystemId, jiraRequest.Issue.Id,
                jiraRequest.Issue.Fields.IssueType.Id);
            if (jiraRequest.WebhookEvent == "jira:issue_deleted" && master != null)
            {
                systemToDbViewModel.MastersToDelete.Add(master);
                return null;
            }
            if (master == null)
            {
                master = new Master
                {
                    RecordDateCreated = DateTime.Now
                };
                systemToDbViewModel.MastersToInsert.Add(master);
            }
            master.RecordDateUpdated = DateTime.Now;
            master.SystemId = syncSystem.SystemId;
            master.ProjectId = jiraRequest.Issue.Fields.Project.Id;
            master.ProjectKey = jiraRequest.Issue.Fields.Project.Key;
            master.ProjectName = jiraRequest.Issue.Fields.Project.Name;

            master.IssueId = jiraRequest.Issue.Id;
            master.IssueKey = jiraRequest.Issue.Key;
            master.IssueTypeId = jiraRequest.Issue.Fields.IssueType.Id;
            master.IssueTypeName = jiraRequest.Issue.Fields.IssueType.Name;
            master.IsSubTask = jiraRequest.Issue.Fields.IssueType.Subtask;
            master.IssueName = GetIssueName(jiraRequest);
            master.ParentEpicKey = jiraEpicLink;
            //master.ParentIssueKey = jiraEpicLink;

            master.OriginalEstimate = jiraRequest.Issue.Fields.TimeOriginalEstimate;
            master.IssueActualWork = jiraRequest.Issue.Fields.TimeSpent;
            master.DateStart = jiraRequest.Issue.Fields.Created;

            if (jiraRequest.Issue.Fields.FixVersions != null && jiraRequest.Issue.Fields.FixVersions.Count != 0)
            {
                master.ParentVersionId = jiraRequest.Issue.Fields.FixVersions[0].Id;
                master.ParentVersionName = jiraRequest.Issue.Fields.FixVersions[0].Name;
                master.ParentVersionReleased = jiraRequest.Issue.Fields.FixVersions[0].Released;
                master.DateRelease = jiraRequest.Issue.Fields.FixVersions[0].ReleaseDate;
            }

            if (jiraRequest.Issue.Fields.Assignee != null)
            {
                master.Assignee = jiraRequest.Issue.Fields.Assignee.Name.ToLower();
            }
            master.IssueStatus = jiraRequest.Issue.Fields.Status.Name;
            if (jiraRequest.Issue.Fields.Parent != null)
            {
                master.ParentIssueId = jiraRequest.Issue.Fields.Parent.Id;
                master.ParentIssueKey = jiraRequest.Issue.Fields.Parent.Key;
            }
            return master;
        }

        //function that directly updates our MasterHistory table in DB
        private MasterHistory FillMasterHistoryIssueAsync(SystemToDbViewModel systemToDbViewModel, JiraRequest jiraRequest, string jiraEpicLink, SyncSystemDTO syncSystem)
        {
            logger.Info("WebHookReceiver Issue MasterHistory");
            if (jiraRequest.WebhookEvent == "jira:issue_deleted")
            {
                return null;
            }
            var masterHistory = new MasterHistory
            {
                RecordDateCreated = DateTime.Now,
                RecordDateUpdated = DateTime.Now,
                SystemId = syncSystem.SystemId,
                ProjectId = jiraRequest.Issue.Fields.Project.Id,
                ProjectKey = jiraRequest.Issue.Fields.Project.Key,
                ProjectName = jiraRequest.Issue.Fields.Project.Name,
                IssueId = jiraRequest.Issue.Id,
                IssueKey = jiraRequest.Issue.Key,
                IssueTypeId = jiraRequest.Issue.Fields.IssueType.Id,
                IssueTypeName = jiraRequest.Issue.Fields.IssueType.Name,
                IsSubTask = jiraRequest.Issue.Fields.IssueType.Subtask,
                IssueName = GetIssueName(jiraRequest),
                ParentEpicKey = jiraEpicLink,
                //ParentIssueKey = jiraEpicLink,
                IssueStatus = jiraRequest.Issue.Fields.Status.Name,
                DateStart = jiraRequest.Issue.Fields.Created
            };

            if (jiraRequest.Issue.Fields.FixVersions != null && jiraRequest.Issue.Fields.FixVersions.Count != 0)
            {
                masterHistory.ParentVersionId = jiraRequest.Issue.Fields.FixVersions[0].Id;
                masterHistory.ParentVersionName = jiraRequest.Issue.Fields.FixVersions[0].Name;
                masterHistory.ParentVersionReleased = jiraRequest.Issue.Fields.FixVersions[0].Released;
                masterHistory.DateRelease = jiraRequest.Issue.Fields.FixVersions[0].ReleaseDate;
            }

            if (jiraRequest.Issue.Fields.Assignee != null)
            {
                masterHistory.Assignee = jiraRequest.Issue.Fields.Assignee.Name.ToLower();
            }
            if (jiraRequest.Issue.Fields.Parent != null)
            {
                masterHistory.ParentIssueId = jiraRequest.Issue.Fields.Parent.Id;
                masterHistory.ParentIssueKey = jiraRequest.Issue.Fields.Parent.Key;
            }
            systemToDbViewModel.MasterHistoriesToInsert.Add(masterHistory);
            return masterHistory;
        }

        //function that directly updates our Staging table in DB
        private Staging FillStagingIssueAsync(SystemToDbViewModel systemToDbViewModel, JiraRequest jiraRequest, string jiraEpicLink,
            SyncSystemDTO syncSystem, List<SyncSystemFieldMapping> syncSystemFieldMappings)
        {
            if (!CheckChangeLog(jiraRequest, syncSystemFieldMappings))
            {
                return null;
            }

            logger.Info("WebHookReceiver Staging");
            if (jiraRequest.WebhookEvent == "jira:issue_deleted")
            {
                return null;
            }

            var staging = new Staging
            {
                RecordDateCreated = DateTime.Now,
                SystemId = syncSystem.SystemId,
                RecordDateUpdated = DateTime.Now,
                RecordState = RecordStateConst.New,
                WebHookEvent = jiraRequest.WebhookEvent,
                ProjectId = jiraRequest.Issue.Fields.Project.Id,
                ProjectKey = jiraRequest.Issue.Fields.Project.Key,
                ProjectName = jiraRequest.Issue.Fields.Project.Name,
                IssueId = jiraRequest.Issue.Id,
                IssueKey = jiraRequest.Issue.Key,
                IssueTypeId = jiraRequest.Issue.Fields.IssueType.Id,
                IssueTypeName = jiraRequest.Issue.Fields.IssueType.Name,
                IsSubTask = jiraRequest.Issue.Fields.IssueType.Subtask,
                IssueName = GetIssueName(jiraRequest),
                ParentEpicKey = jiraEpicLink,
                //ParentIssueKey = jiraEpicLink,
                OriginalEstimate = jiraRequest.Issue.Fields.TimeOriginalEstimate,
                IssueActualWork = jiraRequest.Issue.Fields.TimeSpent,
                IssueStatus = jiraRequest.Issue.Fields.Status.Name,
                DateStart = jiraRequest.Issue.Fields.Created
            };

            GetChangedFields(jiraRequest.ChangeLog?.Items, staging);
            if (jiraRequest.Issue.Fields.FixVersions != null && jiraRequest.Issue.Fields.FixVersions.Count != 0)
            {
                staging.ParentVersionId = jiraRequest.Issue.Fields.FixVersions[0].Id;
                staging.ParentVersionName = jiraRequest.Issue.Fields.FixVersions[0].Name;
                staging.ParentVersionReleased = jiraRequest.Issue.Fields.FixVersions[0].Released;
                staging.DateRelease = jiraRequest.Issue.Fields.FixVersions[0].ReleaseDate;
            }

            if (jiraRequest.Issue.Fields.Assignee != null)
            {
                staging.Assignee = jiraRequest.Issue.Fields.Assignee.Name.ToLower();
            }
            if (jiraRequest.Issue.Fields.Parent != null)
            {
                staging.ParentIssueId = jiraRequest.Issue.Fields.Parent.Id;
                staging.ParentIssueKey = jiraRequest.Issue.Fields.Parent.Key;
            }
            systemToDbViewModel.StagingsToInsert.Add(staging);
            return staging;
        }

        private void FillWorkLog(SystemToDbViewModel systemToDbViewModel, JiraRequest jiraRequest, int systemId)
        {
            MasterWorklog masterWorklog = masterWorklogBusinessService
                .GetWorkLogById(systemId, jiraRequest.Worklog.Id);

            if (masterWorklog == null)
            {
                masterWorklog = new MasterWorklog
                {
                    RecordDateCreated = DateTime.Now,
                };
                systemToDbViewModel.MasterWorklogsToInsert.Add(masterWorklog);
            }

            masterWorklog.RecordDateUpdated = DateTime.Now;
            masterWorklog.RecordState = RecordStateConst.New;
            masterWorklog.WebHookEvent = jiraRequest.WebhookEvent;
            masterWorklog.SystemId = systemId;
            masterWorklog.WorkLogId = jiraRequest.Worklog.Id;
            masterWorklog.IssueId = jiraRequest.Worklog.IssueId;
            masterWorklog.TimeSpentSeconds = jiraRequest.WebhookEvent == "worklog_deleted" ? 0 : jiraRequest.Worklog.TimeSpentSeconds;
            masterWorklog.DateStarted = DateTime.Parse(jiraRequest.Worklog.Started);
            masterWorklog.DateCreated = DateTime.Parse(jiraRequest.Worklog.Created);
            masterWorklog.DateUpdated = DateTime.Parse(jiraRequest.Worklog.Updated);
            masterWorklog.Comment = jiraRequest.Worklog.Comment;
            masterWorklog.AuthorName = jiraRequest.Worklog.Author.Name.ToLower();
            masterWorklog.AuthorEmailAddress = jiraRequest.Worklog.Author.EmailAddress.ToLower();
            masterWorklog.AuthorKey = jiraRequest.Worklog.Author.Name.ToLower();
        }

        private Master FillMasterVersionAsync(SystemToDbViewModel systemToDbViewModel, JiraRequest jiraRequest, int systemId)
        {
            logger.Info("WebHookReceiver Version Master");
            Master master = masterBusinessService.GetIssueById(systemId, jiraRequest.Version.Id, "version");

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

            master.IssueId = jiraRequest.Version.Id;
            master.IssueKey = jiraRequest.Version.Id;
            master.IssueName = GetVersionName(jiraRequest);
            master.ProjectId = jiraRequest.Version.ProjectId.ToString();
            master.ParentVersionReleased = jiraRequest.Version.Released;
            master.IssueTypeName = "version";
            master.IssueTypeId = "version";
            master.DateFinish = jiraRequest.Version.UserReleaseDate;
            master.DateRelease = jiraRequest.Version.UserReleaseDate;
            //await unitOfWork.SaveChangesAsync();
            return master;
        }

        private MasterHistory FillMasterHistoryVersionAsync(SystemToDbViewModel systemToDbViewModel, JiraRequest jiraRequest, int systemId)
        {
            logger.Info("WebHookReceiver Version MasterHistory");
            var masterHistory = new MasterHistory
            {
                RecordDateCreated = DateTime.Now
            };
            systemToDbViewModel.MasterHistoriesToInsert.Add(masterHistory);

            masterHistory.RecordDateUpdated = DateTime.Now;
            masterHistory.SystemId = systemId;

            masterHistory.IssueId = jiraRequest.Version.Id;
            masterHistory.IssueName = GetVersionName(jiraRequest);
            masterHistory.IssueKey = jiraRequest.Version.Name.Trim();
            masterHistory.ProjectId = jiraRequest.Version.ProjectId.ToString();
            masterHistory.ParentVersionReleased = jiraRequest.Version.Released;
            masterHistory.IssueTypeName = "version";
            masterHistory.IssueTypeId = "version";
            masterHistory.DateFinish = jiraRequest.Version.UserReleaseDate;
            masterHistory.DateRelease = jiraRequest.Version.UserReleaseDate;
            //await unitOfWork.SaveChangesAsync();
            return masterHistory;
        }

        private Staging FillStagingVersionAsync(SystemToDbViewModel systemToDbViewModel, JiraRequest jiraRequest,
            int systemId, List<SyncSystemFieldMapping> syncSystemFieldMappings)
        {
            if (!CheckChangeLog(jiraRequest, syncSystemFieldMappings))
            {
                return null;
            }

            logger.Info("WebHookReceiver Version Master");
            var staging = new Staging
            {
                RecordDateCreated = DateTime.Now,
                RecordDateUpdated = DateTime.Now,
                RecordState = RecordStateConst.New,
                WebHookEvent = jiraRequest.WebhookEvent,
                SystemId = systemId,
                IssueId = jiraRequest.Version.Id,
                IssueKey = jiraRequest.Version.Id,
                IssueName = GetVersionName(jiraRequest),
                ProjectId = jiraRequest.Version.ProjectId.ToString(),
                ParentVersionReleased = jiraRequest.Version.Released,
                IssueTypeName = "version",
                IssueTypeId = "version",
                DateFinish = jiraRequest.Version.UserReleaseDate,
                DateRelease = jiraRequest.Version.UserReleaseDate
            };

            //await unitOfWork.SaveChangesAsync();
            systemToDbViewModel.StagingsToInsert.Add(staging);
            return staging;
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

        private Dictionary<string, object> GetIssueFieldsDictionary(string json)
        {
            var requestDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            if (requestDictionary.ContainsKey("issue"))
            {
                string issueJson = requestDictionary["issue"].ToString();
                var issueDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(issueJson);
                string fieldsJson = issueDictionary["fields"].ToString();
                return JsonConvert.DeserializeObject<Dictionary<string, object>>(fieldsJson);
            }
            return new Dictionary<string, object>();
        }

        private string GetChangedFields(List<JiraChangeLogItem> items, Staging staging = null)
        {
            if (items == null)
            {
                return null;
            }
            if (staging == null)
            {
                return items.Aggregate("", (current, jiraChangeLogItem) => current + jiraChangeLogItem.Field + "|").Trim('|');
            }
            if (String.IsNullOrEmpty(staging.ChangedFields))
            {
                staging.ChangedFields = items.Aggregate("", (current, jiraChangeLogItem) => current + jiraChangeLogItem.Field + "|").Trim('|');
            }
            else
            {
                foreach (JiraChangeLogItem jiraChangeLogItem in items)
                {
                    if (!staging.ChangedFields.Contains(jiraChangeLogItem.Field))
                    {
                        staging.ChangedFields += "|" + jiraChangeLogItem.Field;
                    }
                }
            }
            return staging.ChangedFields;
        }

        private void HandleException(Exception exception)
        {
            logger.Fatal(exception);
        }

        public void HandleException(string message, bool needLog)
        {
            if (needLog)
            {
                logger.Fatal(message);
            }
        }

        private string GetVersionName(JiraRequest jiraRequest)
        {
            string name = jiraRequest.Version.Name.RemoveNewLinesTabs().Truncate();
            return name;
        }

        private string GetIssueName(JiraRequest jiraRequest)
        {
            string issueName = jiraRequest.Issue.Fields.Summary.RemoveNewLinesTabs().Truncate();
            return issueName;
        }
    }
}