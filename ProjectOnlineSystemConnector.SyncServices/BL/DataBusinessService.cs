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
using ProjectOnlineSystemConnector.BusinessServices;
using ProjectOnlineSystemConnector.Common;
using ProjectOnlineSystemConnector.Data.Views.EntityClasses;
using ProjectOnlineSystemConnector.DataAccess.Database.UnitOfWork;
using ProjectOnlineSystemConnector.DataModel.DTO;
using ProjectOnlineSystemConnector.DataAccess.CSOM;
using ProjectOnlineSystemConnector.DataModel.OData;
using ProjectOnlineSystemConnector.SyncServices.DataModel;

namespace ProjectOnlineSystemConnector.SyncServices.BL
{
    class DataBusinessService
    {
        public static DataFromDbViewModel GetDataFromDbViewModel(ProjectOnlineODataService projectOnlineODataService,
            UnitOfWork unitOfWork, bool isWorklogs)
        {
            StagingBusinessService stagingBusinessService = new StagingBusinessService(unitOfWork);
            SyncSystemBusinessService syncSystemBusinessService = new SyncSystemBusinessService(unitOfWork);
            ProjectServerSystemLinkBusinessService projectServerSystemLinkBusinessService = new ProjectServerSystemLinkBusinessService(projectOnlineODataService, unitOfWork);
            MasterWorklogBusinessService masterWorklogBusinessService = new MasterWorklogBusinessService(unitOfWork);
            MasterBusinessService masterBusinessService = new MasterBusinessService(unitOfWork);

            DateTime startDateTimesheetPeriods = projectOnlineODataService.GetODataTimesheetPeriods().Select(x => x.StartDate).Min();
            DateTime endDateTimesheetPeriods = projectOnlineODataService.GetODataTimesheetPeriods().Select(x => x.EndDate).Max();

            DataFromDbViewModel dataFromDbViewModel = new DataFromDbViewModel
            {
                SyncSystems = syncSystemBusinessService.GetSyncSystemList(),
                ProjectServerSystemLinks = projectServerSystemLinkBusinessService.GetList()
            };

            stagingBusinessService.RemoveDoneStagings();
            dataFromDbViewModel.StagingsAll = stagingBusinessService.GetAllStagings(isWorklogs);

            dataFromDbViewModel.MasterWorklogs = masterWorklogBusinessService.GetActualMasterWorklogs(dataFromDbViewModel.StagingsAll,
                dataFromDbViewModel.SyncSystems, startDateTimesheetPeriods, endDateTimesheetPeriods);

            List<int> stagingIds = dataFromDbViewModel.StagingsAll.Select(x => x.StagingId).ToList();
            //stagingBusinessService.SetStagingsRecordState(stagingIds,
            //    isWorklogs ? ProjectServerConstants.RecordStateActual : ProjectServerConstants.RecordStateGeneral,
            //    RecordStateConst.Pending);

            dataFromDbViewModel.CustomValuesAll = stagingBusinessService
                .GetCustomValues(stagingIds);

            List<VStagingFieldMappingValue> lastUpdateUsersCustomValues = dataFromDbViewModel.CustomValuesAll
                .Where(x => x.StagingId.HasValue
                            && stagingIds.Contains(x.StagingId.Value)
                            && x.EpmFieldName == ProjectServerConstants.LastUpdateUser)
                .ToList();
            List<AssignmentInfo> assignees = dataFromDbViewModel.StagingsAll
                .Where(x => !String.IsNullOrEmpty(x.Assignee))
                .Select(x => new AssignmentInfo
                {
                    IssueKey = x.IssueKey,
                    IssueId = x.IssueId,
                    SystemId = x.SystemId,
                    AuthorKey = x.Assignee
                }).ToList();
            List<AssignmentInfo> lastUpdateUsers = (from staging in dataFromDbViewModel.StagingsAll
                join lu in lastUpdateUsersCustomValues on staging.StagingId equals lu.StagingId
                select new AssignmentInfo
                {
                    IssueKey = staging.IssueKey,
                    IssueId = staging.IssueId,
                    SystemId = staging.SystemId,
                    AuthorKey = lu.Value
                }).ToList();
            List<AssignmentInfo> worklogUsers = (from staging in dataFromDbViewModel.StagingsAll
                join worklog in dataFromDbViewModel.MasterWorklogs on new { staging.SystemId, staging.IssueId } equals
                new { worklog.SystemId, worklog.IssueId }
                select new AssignmentInfo
                {
                    IssueKey = staging.IssueKey,
                    IssueId = staging.IssueId,
                    SystemId = staging.SystemId,
                    AuthorKey = worklog.AuthorKey
                }).ToList();

            dataFromDbViewModel.Assignments = lastUpdateUsers.Union(assignees).Union(worklogUsers).Distinct(new AssignmentInfoComparer()).ToList();
            foreach (AssignmentInfo assignmentInfo in dataFromDbViewModel.Assignments)
            {
                ODataResource resource = projectOnlineODataService.GetODataResource(assignmentInfo.AuthorKey);
                if (resource != null)
                {
                    assignmentInfo.ResourceUid = resource.ResourceId;
                }
            }
            dataFromDbViewModel.Assignments = dataFromDbViewModel.Assignments.Where(x => x.ResourceUid != Guid.Empty).ToList();

            //List<string> issueKeysCurrentLevel = dataFromDbViewModel.StagingsAll
            //    .Where(x => x.IsSubTask && !String.IsNullOrEmpty(x.ParentIssueKey))
            //    .Select(x => x.ParentIssueKey).Distinct().ToList();
            dataFromDbViewModel.ParentIssues = new List<Master>();
            List<string> issueKeysCurrentLevel = dataFromDbViewModel.StagingsAll
                .Where(x => !String.IsNullOrEmpty(x.ParentIssueKey))
                .Select(x => x.ParentIssueKey).Distinct().ToList();
            List<Master> parentIssuesCurrentLevel = masterBusinessService.GetMasters(issueKeysCurrentLevel);
            dataFromDbViewModel.ParentIssues.AddRange(parentIssuesCurrentLevel);
            while (parentIssuesCurrentLevel.Count != 0)
            {
                issueKeysCurrentLevel = parentIssuesCurrentLevel
                    .Where(x => !String.IsNullOrEmpty(x.ParentIssueKey))
                    .Select(x => x.ParentIssueKey).Distinct().ToList();
                parentIssuesCurrentLevel = masterBusinessService.GetMasters(issueKeysCurrentLevel);
                dataFromDbViewModel.ParentIssues.AddRange(parentIssuesCurrentLevel);
            }

            return dataFromDbViewModel;
        }

    }
}
