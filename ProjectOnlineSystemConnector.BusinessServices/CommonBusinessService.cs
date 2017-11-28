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
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ProjectOnlineSystemConnector.Common;
using ProjectOnlineSystemConnector.Data.Views.EntityClasses;
using ProjectOnlineSystemConnector.DataAccess.Database.UnitOfWork;
using ProjectOnlineSystemConnector.DataModel.JiraWebHook;
using ProjectOnlineSystemConnector.DataModel.ViewModel;

namespace ProjectOnlineSystemConnector.BusinessServices
{
    public class CommonBusinessService : BaseBusinessService
    {
        public CommonBusinessService(UnitOfWork unitOfWork) : base(unitOfWork)
        {
        }

        public void SetSprintField(JiraSprint resultSprint, List<SyncSystemFieldMapping> syncSystemFieldMappings,
            Master master, MasterHistory masterHistory, Staging staging,
            List<MasterFieldMappingValue> masterFieldMappingValues,
            List<MasterHistoryFieldMappingValue> masterHistoryFieldMappingValues,
            List<StagingFieldMappingValue> stagingFieldMappingValues)
        {
            if (resultSprint != null)
            {
                Logger.Info($"ParseSprintField resultSprint.Id: {resultSprint.Id} " +
                            $"resultSprint.Name: {resultSprint.Name} " +
                            $"resultSprint.State: {resultSprint.State} " +
                            $"resultSprint.StartDate: {resultSprint.StartDate} " +
                            $"resultSprint.EndDate: {resultSprint.EndDate}");

                //we getting Ids for mapping frob DB 
                int sprintIdSyncSystemFieldMappingId =
                    syncSystemFieldMappings.Where(x => x.EpmFieldName == ProjectServerConstants.SprintId)
                        .Select(y => y.SyncSystemFieldMappingId)
                        .FirstOrDefault();
                int sprintNameSyncSystemFieldMappingId =
                    syncSystemFieldMappings.Where(
                        x => x.EpmFieldName == ProjectServerConstants.SprintName)
                        .Select(y => y.SyncSystemFieldMappingId)
                        .FirstOrDefault();
                int sprintStateSyncSystemFieldMappingId =
                    syncSystemFieldMappings.Where(
                        x => x.EpmFieldName == ProjectServerConstants.SprintState)
                        .Select(y => y.SyncSystemFieldMappingId)
                        .FirstOrDefault();
                int sprintStartDateSyncSystemFieldMappingId =
                    syncSystemFieldMappings.Where(
                        x => x.EpmFieldName == ProjectServerConstants.SprintStartDate)
                        .Select(y => y.SyncSystemFieldMappingId)
                        .FirstOrDefault();
                int sprintEndDateSyncSystemFieldMappingId =
                    syncSystemFieldMappings.Where(
                        x => x.EpmFieldName == ProjectServerConstants.SprintEndDate)
                        .Select(y => y.SyncSystemFieldMappingId)
                        .FirstOrDefault();
                if (sprintIdSyncSystemFieldMappingId == 0 || sprintNameSyncSystemFieldMappingId == 0
                    || sprintStateSyncSystemFieldMappingId == 0 || sprintStartDateSyncSystemFieldMappingId == 0
                    || sprintEndDateSyncSystemFieldMappingId == 0)
                {
                    return;
                }
                //Id
                master.ParentSprintId = resultSprint.Id;
                masterHistory.ParentSprintId = resultSprint.Id;
                masterFieldMappingValues.Add(new MasterFieldMappingValue
                {
                    Master = master,
                    SyncSystemFieldMappingId = sprintIdSyncSystemFieldMappingId,
                    Value = resultSprint.Id
                });
                masterHistoryFieldMappingValues.Add(new MasterHistoryFieldMappingValue
                {
                    MasterHistory = masterHistory,
                    SyncSystemFieldMappingId = sprintIdSyncSystemFieldMappingId,
                    Value = resultSprint.Id
                });
                if (staging != null)
                {
                    //Id
                    staging.ParentSprintId = resultSprint.Id;
                    stagingFieldMappingValues.Add(new StagingFieldMappingValue
                    {
                        Staging = staging,
                        SyncSystemFieldMappingId = sprintIdSyncSystemFieldMappingId,
                        Value = resultSprint.Id
                    });
                    //Name
                    staging.ParentSprintName = resultSprint.Name;
                    stagingFieldMappingValues.Add(new StagingFieldMappingValue
                    {
                        Staging = staging,
                        SyncSystemFieldMappingId = sprintNameSyncSystemFieldMappingId,
                        Value = resultSprint.Name
                    });
                    //state
                    stagingFieldMappingValues.Add(new StagingFieldMappingValue
                    {
                        Staging = staging,
                        SyncSystemFieldMappingId = sprintStateSyncSystemFieldMappingId,
                        Value = resultSprint.State
                    });
                    //startdate
                    stagingFieldMappingValues.Add(new StagingFieldMappingValue
                    {
                        Staging = staging,
                        SyncSystemFieldMappingId = sprintStartDateSyncSystemFieldMappingId,
                        Value = resultSprint.StartDate
                    });
                    //enddate
                    stagingFieldMappingValues.Add(new StagingFieldMappingValue
                    {
                        Staging = staging,
                        SyncSystemFieldMappingId = sprintEndDateSyncSystemFieldMappingId,
                        Value = resultSprint.EndDate
                    });

                }
                //Name
                master.ParentSprintName = resultSprint.Name;
                masterHistory.ParentSprintName = resultSprint.Name;
                masterFieldMappingValues.Add(new MasterFieldMappingValue
                {
                    Master = master,
                    SyncSystemFieldMappingId = sprintNameSyncSystemFieldMappingId,
                    Value = resultSprint.Name
                });
                masterHistoryFieldMappingValues.Add(new MasterHistoryFieldMappingValue
                {
                    MasterHistory = masterHistory,
                    SyncSystemFieldMappingId = sprintNameSyncSystemFieldMappingId,
                    Value = resultSprint.Name
                });
                //state
                masterFieldMappingValues.Add(new MasterFieldMappingValue
                {
                    Master = master,
                    SyncSystemFieldMappingId = sprintStateSyncSystemFieldMappingId,
                    Value = resultSprint.State
                });
                masterHistoryFieldMappingValues.Add(new MasterHistoryFieldMappingValue
                {
                    MasterHistory = masterHistory,
                    SyncSystemFieldMappingId = sprintStateSyncSystemFieldMappingId,
                    Value = resultSprint.State
                });
                //startdate
                masterFieldMappingValues.Add(new MasterFieldMappingValue
                {
                    Master = master,
                    SyncSystemFieldMappingId = sprintStartDateSyncSystemFieldMappingId,
                    Value = resultSprint.StartDate
                });
                masterHistoryFieldMappingValues.Add(new MasterHistoryFieldMappingValue
                {
                    MasterHistory = masterHistory,
                    SyncSystemFieldMappingId = sprintStartDateSyncSystemFieldMappingId,
                    Value = resultSprint.StartDate
                });
                //enddate
                masterFieldMappingValues.Add(new MasterFieldMappingValue
                {
                    Master = master,
                    SyncSystemFieldMappingId = sprintEndDateSyncSystemFieldMappingId,
                    Value = resultSprint.EndDate
                });
                masterHistoryFieldMappingValues.Add(new MasterHistoryFieldMappingValue
                {
                    MasterHistory = masterHistory,
                    SyncSystemFieldMappingId = sprintEndDateSyncSystemFieldMappingId,
                    Value = resultSprint.EndDate
                });

                Logger.Info($"ParseSprintField stagingFieldMappingValues.Count: {stagingFieldMappingValues.Count} " +
                            $"masterFieldMappingValues.Count: {masterFieldMappingValues.Count} " +
                            $"masterHistoryFieldMappingValues.Count: {masterHistoryFieldMappingValues.Count}");

            }
        }

        public async Task AddNewData(SystemToDbViewModel systemToDbViewModel)
        {
            Logger.Info($"SyncAll AddNewData MastersToInsert {systemToDbViewModel.MastersToInsert.Count}");
            systemToDbViewModel.MastersToInsert = UnitOfWork.MasterRepository.AddRange(systemToDbViewModel.MastersToInsert);
            Logger.Info($"SyncAll AddNewData MastersToDelete {systemToDbViewModel.MastersToDelete.Count}");
            systemToDbViewModel.MastersToDelete = UnitOfWork.MasterRepository.RemoveRange(systemToDbViewModel.MastersToDelete);
            Logger.Info($"SyncAll AddNewData MasterWorklogsToInsert {systemToDbViewModel.MasterWorklogsToInsert.Count}");
            systemToDbViewModel.MasterWorklogsToInsert = UnitOfWork.MasterWorklogRepository.AddRange(systemToDbViewModel.MasterWorklogsToInsert);
            Logger.Info($"SyncAll AddNewData StagingsToInsert {systemToDbViewModel.StagingsToInsert.Count}");
            systemToDbViewModel.StagingsToInsert = UnitOfWork.StagingRepository.AddRange(systemToDbViewModel.StagingsToInsert);
            Logger.Info($"SyncAll AddNewData MasterHistoriesToInsert {systemToDbViewModel.MasterHistoriesToInsert.Count}");
            systemToDbViewModel.MasterHistoriesToInsert = UnitOfWork.MasterHistoryRepository.AddRange(systemToDbViewModel.MasterHistoriesToInsert);
            Logger.Info($"SyncAll AddNewData MasterFieldMappingValuesToInsert {systemToDbViewModel.MasterFieldMappingValuesToInsert.Count}");
            systemToDbViewModel.MasterFieldMappingValuesToInsert = UnitOfWork.MasterFieldMappingValueRepository.AddRange(systemToDbViewModel.MasterFieldMappingValuesToInsert);
            Logger.Info($"SyncAll AddNewData StagingFieldMappingValuesToInsert {systemToDbViewModel.StagingFieldMappingValuesToInsert.Count}");
            systemToDbViewModel.StagingFieldMappingValuesToInsert = UnitOfWork.StagingFieldMappingValueRepository.AddRange(systemToDbViewModel.StagingFieldMappingValuesToInsert);
            Logger.Info($"SyncAll AddNewData MasterHistoryFieldMappingValuesToInsert {systemToDbViewModel.MasterHistoryFieldMappingValuesToInsert.Count}");
            systemToDbViewModel.MasterHistoryFieldMappingValuesToInsert = UnitOfWork.MasterHistoryFieldMappingValueRepository.AddRange(systemToDbViewModel.MasterHistoryFieldMappingValuesToInsert);
            Logger.Info("SyncAll AddNewData SaveChangesAsync");
            await UnitOfWork.SaveChangesAsync();
        }

        public void DeleteOldData(SystemToDbViewModel systemToDbViewModel, int systemId)
        {
            try
            {
                Stopwatch stopwatch = new Stopwatch();
                Logger.Info($"DeleteOldData 1 stopwatch: {stopwatch.ElapsedMilliseconds}");

                List<int> newStagingIds = systemToDbViewModel.StagingsToInsert.Select(x => x.StagingId)
                    .Distinct()
                    .ToList();
                List<int> newMasterIds = systemToDbViewModel.MastersToInsert.Select(x => x.MasterId).Distinct().ToList();
                List<int> newMasterWorklogIds =
                    systemToDbViewModel.MasterWorklogsToInsert.Select(x => x.MasterWorklogId).Distinct().ToList();
                List<string> issueKeys = systemToDbViewModel.MastersToInsert.Select(x => x.IssueKey).Distinct().ToList();
                List<string> issueIds =
                    systemToDbViewModel.MasterWorklogsToInsert.Select(x => x.IssueId).Distinct().ToList();

                Logger.Info($"DeleteOldData 2 stopwatch: {stopwatch.ElapsedMilliseconds}");

                Logger.Info($"SyncAll DeleteOldData systemId: {systemId} issueKeys: {issueKeys.Count}; " +
                            $"issueIds: {issueIds.Count}; newMasterWorklogIds: {newMasterWorklogIds.Count}; " +
                            $"newStagingIds: {newStagingIds.Count}; newMasterIds: {newMasterIds.Count};");
                UnitOfWork.MasterRepository
                    .RemoveRange(x => x.SystemId == systemId && issueKeys.Contains(x.IssueKey)
                                      && !newMasterIds.Contains(x.MasterId));
                UnitOfWork.StagingRepository
                    .RemoveRange(x => x.SystemId == systemId && issueKeys.Contains(x.IssueKey)
                                      && !newStagingIds.Contains(x.StagingId));
                UnitOfWork.MasterWorklogRepository
                    .RemoveRange(x => x.SystemId == systemId && issueIds.Contains(x.IssueId)
                                      && !newMasterWorklogIds.Contains(x.MasterWorklogId));
                Logger.Info($"DeleteOldData 4 stopwatch: {stopwatch.ElapsedMilliseconds}");
            }
            catch (Exception exception)
            {
                Logger.Warn(exception);
            }
        }
    }
}