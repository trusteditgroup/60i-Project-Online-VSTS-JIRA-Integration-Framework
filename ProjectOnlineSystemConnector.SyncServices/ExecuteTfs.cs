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
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using ProjectOnlineSystemConnector.BusinessServices;
using ProjectOnlineSystemConnector.Common;
using ProjectOnlineSystemConnector.Data.Views.EntityClasses;
using ProjectOnlineSystemConnector.DataAccess.Database.UnitOfWork;
using ProjectOnlineSystemConnector.DataModel.Common;
using ProjectOnlineSystemConnector.DataModel.DTO;
using ProjectOnlineSystemConnector.DataModel.ViewModel;

namespace ProjectOnlineSystemConnector.SyncServices
{
    public class ExecuteTfs
    {
        private readonly SyncSystemBusinessService syncSystemBusinessService;
        private readonly CommonBusinessService commonBusinessService;
        private readonly SyncSystemFieldMappingBusinessService syncSystemFieldMappingBusinessService;

        public ExecuteTfs(UnitOfWork unitOfWork)
        {
            syncSystemBusinessService = new SyncSystemBusinessService(unitOfWork);
            commonBusinessService = new CommonBusinessService(unitOfWork);
            syncSystemFieldMappingBusinessService = new SyncSystemFieldMappingBusinessService(unitOfWork);
        }

        public async Task<ProxyResponse> Execute(int systemId, int itemId, string projectId)
        {
            SystemToDbViewModel syncAllViewModel = new SystemToDbViewModel();

            SyncSystemDTO syncSystem = await syncSystemBusinessService.GetSyncSystemAsync(systemId);

            List<SyncSystemFieldMapping> syncSystemFieldMappings = syncSystemFieldMappingBusinessService.GetSyncSystemFieldMappings(syncSystem.SystemId);

            VssConnection connection = new VssConnection(new Uri(syncSystem.SystemUrl + "/DefaultCollection"),
                new VssBasicCredential(String.Empty, syncSystem.SystemPassword));
            // Create instance of WorkItemTrackingHttpClient using VssConnection
            WorkItemTrackingHttpClient witClient = connection.GetClient<WorkItemTrackingHttpClient>();
                        
            List<WorkItem> epics = new List<WorkItem>();            

            if (itemId == 0)
            {
                Wiql wiQuery = new Wiql()
                {
                    Query =
                    "Select [System.Id] "
                    + "from WorkItems"
                    + " Where [System.WorkItemType] = 'Epic'"
                    + $" And [System.TeamProject] = '{projectId}'"
                };
                var items = await witClient.QueryByWiqlAsync(wiQuery);

                List<int> epicsIds = items.WorkItems.Select(x => x.Id).ToList();
                List<WorkItem> masterItems = await witClient.GetWorkItemsAsync(epicsIds, expand: WorkItemExpand.Relations);
                epics.AddRange(masterItems);
            }
            else
            {
                WorkItem masterItem = await witClient.GetWorkItemAsync(itemId, expand: WorkItemExpand.Relations);
                epics.Add(masterItem);
            }

            


            await GetDataFromTfs(syncAllViewModel, epics, systemId, syncSystemFieldMappings, witClient);

            await commonBusinessService.AddNewData(syncAllViewModel);
            commonBusinessService.DeleteOldData(syncAllViewModel, syncSystem.SystemId);

            return new ProxyResponse
            {
                Result = "ok",
                Data = "TFS Execute OK"
            };
        }

        private async Task GetDataFromTfs(SystemToDbViewModel syncAllViewModel, List<WorkItem> workItems,
            int systemId, List<SyncSystemFieldMapping> syncSystemFieldMappings,
            WorkItemTrackingHttpClient witClient)
        {
            List<int> childItemIds = new List<int>();
            foreach (WorkItem workItem in workItems)
            {
                FillSyncAllRecord(syncAllViewModel, workItem, systemId, syncSystemFieldMappings);

                if (workItem.Relations != null)
                {
                    foreach (var relationE in workItem.Relations)
                    {
                        if (relationE.Rel == "System.LinkTypes.Hierarchy-Forward")
                        {
                            int childItemId = int.Parse(relationE.Url.Substring(relationE.Url.LastIndexOf("/", StringComparison.Ordinal) + 1));
                            childItemIds.Add(childItemId);
                        }
                    }
                }
                
            }
            if (childItemIds.Count == 0)
            {
                return;
            }
            List<WorkItem> childItems =
                await witClient.GetWorkItemsAsync(childItemIds, expand: WorkItemExpand.Relations);
            if (childItems.Count == 0)
            {
                return;
            }
            await GetDataFromTfs(syncAllViewModel, childItems, systemId, syncSystemFieldMappings, witClient);
        }

        void FillSyncAllRecord(SystemToDbViewModel syncAllViewModel, WorkItem workItem, int systemId, List<SyncSystemFieldMapping> syncSystemFieldMappings)
        {
            Staging staging = FillSyncAllStagingTable(syncAllViewModel, workItem, systemId);
            Master master = FillSyncAllMasterTable(syncAllViewModel, workItem, systemId);

            SyncSystemFieldMapping fieldMappingRecordStateGeneral = syncSystemFieldMappings
                .FirstOrDefault(x => x.SystemId == systemId &&
                                     x.EpmFieldName == ProjectServerConstants.RecordStateGeneral);
            FillFieldMappingValueTables(syncAllViewModel, staging, master, fieldMappingRecordStateGeneral, RecordStateConst.New);

            SyncSystemFieldMapping fieldMappingRecordStateActual = syncSystemFieldMappings
                .FirstOrDefault(x => x.SystemId == systemId &&
                                     x.EpmFieldName == ProjectServerConstants.RecordStateActual);
            FillFieldMappingValueTables(syncAllViewModel, staging, master, fieldMappingRecordStateActual, RecordStateConst.Done);
        }

        Staging FillSyncAllStagingTable(SystemToDbViewModel syncAllViewModel, WorkItem workItem, int systemId)
        {
            Staging staging = new Staging
            {
                RecordDateCreated = DateTime.Now,
                SystemId = systemId,
                ChangedFields = "all",
                RecordDateUpdated = DateTime.Now,
                RecordState = RecordStateConst.New,
                WebHookEvent = "syncAll"
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
            if (workItem.Fields.ContainsKey("System.AssignedTo"))
            {
                if (workItem.Fields["System.AssignedTo"] != null)
                {
                    staging.Assignee = workItem.Fields["System.AssignedTo"].ToString()
                        .Substring(workItem.Fields["System.AssignedTo"].ToString()
                            .IndexOf("<", StringComparison.Ordinal)).Trim('<', '>');
                }
            }
            if (workItem.Relations != null)
            {
                WorkItemRelation relationCrt = workItem.Relations
                    .FirstOrDefault(x => x.Rel == "System.LinkTypes.Hierarchy-Reverse");
                if (relationCrt != null)
                {
                    string parentKey = relationCrt.Url.Substring(relationCrt.Url.LastIndexOf("/", StringComparison.Ordinal) + 1);
                    Staging parentStaging = syncAllViewModel.StagingsToInsert.FirstOrDefault(x => x.IssueKey == parentKey);
                    if (parentStaging != null)
                    {
                        if (parentStaging.IssueTypeName == "Epic")
                        {
                            staging.ParentEpicId = parentKey;
                            staging.ParentEpicKey = parentKey;
                        }
                        else
                        {
                            staging.ParentIssueId = parentKey;
                            staging.ParentIssueKey = parentKey;
                        }
                    }
                }
                else
                {
                    staging.ParentIssueId = null;
                    staging.ParentIssueKey = null;
                }
            }

            syncAllViewModel.StagingsToInsert.Add(staging);
            return staging;
        }

        Master FillSyncAllMasterTable(SystemToDbViewModel syncAllViewModel, WorkItem workItem, int systemId)
        {
            Master master = new Master
            {
                RecordDateCreated = DateTime.Now,
                SystemId = systemId,
                RecordDateUpdated = DateTime.Now
            };


            if (workItem.Fields.ContainsKey("System.TeamProject"))
            {
                master.ProjectId = (string)workItem.Fields["System.TeamProject"];
                master.ProjectKey = (string)workItem.Fields["System.TeamProject"];
                master.ProjectName = (string)workItem.Fields["System.TeamProject"];
            }
            master.IssueId = workItem.Id?.ToString();
            master.IssueKey = workItem.Id?.ToString();
            if (workItem.Fields.ContainsKey("System.WorkItemType"))
            {
                master.IssueTypeId = workItem.Fields["System.WorkItemType"].ToString();
                master.IssueTypeName = workItem.Fields["System.WorkItemType"].ToString();
            }
            if (workItem.Fields.ContainsKey("System.Title"))
            {
                master.IssueName = workItem.Fields["System.Title"].ToString();
            }
            if (workItem.Fields.ContainsKey("System.IterationId"))
            {
                master.ParentVersionId = workItem.Fields["System.IterationId"].ToString();
            }
            if (workItem.Fields.ContainsKey("System.IterationLevel2"))
            {
                master.ParentVersionName = workItem.Fields["System.IterationLevel2"].ToString();
            }
            if (workItem.Fields.ContainsKey("System.AssignedTo"))
            {
                if (workItem.Fields["System.AssignedTo"] != null)
                {
                    master.Assignee = workItem.Fields["System.AssignedTo"].ToString()
                        .Substring(workItem.Fields["System.AssignedTo"].ToString()
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
                        master.ParentEpicId = relationCrt.Url.Substring(relationCrt.Url.LastIndexOf("/", StringComparison.Ordinal) + 1);
                        master.ParentEpicKey = relationCrt.Url.Substring(relationCrt.Url.LastIndexOf("/", StringComparison.Ordinal) + 1);
                    }
                    else
                    {
                        master.ParentIssueId = relationCrt.Url.Substring(relationCrt.Url.LastIndexOf("/", StringComparison.Ordinal) + 1);
                        master.ParentIssueKey = relationCrt.Url.Substring(relationCrt.Url.LastIndexOf("/", StringComparison.Ordinal) + 1);
                    }

                }
                else
                {
                    master.ParentIssueId = null;
                    master.ParentIssueKey = null;
                }
            }

            syncAllViewModel.MastersToInsert.Add(master);
            return master;
        }

        void FillFieldMappingValueTables(SystemToDbViewModel syncAllViewModel, Staging staging, Master master, SyncSystemFieldMapping fieldMapping, string customFieldValue)
        {
            StagingFieldMappingValue stagingValue = new StagingFieldMappingValue
            {
                Staging = staging,
                SyncSystemFieldMappingId = fieldMapping.SyncSystemFieldMappingId,
                Value = customFieldValue
            };
            syncAllViewModel.StagingFieldMappingValuesToInsert.Add(stagingValue);

            MasterFieldMappingValue masterValue = new MasterFieldMappingValue
            {
                Master = master,
                SyncSystemFieldMappingId = fieldMapping.SyncSystemFieldMappingId,
                Value = customFieldValue
            };
            syncAllViewModel.MasterFieldMappingValuesToInsert.Add(masterValue);
        }
    }
}