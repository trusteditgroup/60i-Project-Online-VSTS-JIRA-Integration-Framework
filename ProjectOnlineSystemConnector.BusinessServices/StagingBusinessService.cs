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
using ProjectOnlineSystemConnector.Common;
using ProjectOnlineSystemConnector.Data.Views.EntityClasses;
using ProjectOnlineSystemConnector.DataAccess.Database.UnitOfWork;
using ProjectOnlineSystemConnector.DataModel.DTO;
using Z.EntityFramework.Plus;

namespace ProjectOnlineSystemConnector.BusinessServices
{
    public class StagingBusinessService : BaseBusinessService
    {
        public StagingBusinessService(UnitOfWork unitOfWork) : base(unitOfWork)
        {
        }

        public void RemoveDoneStagings()
        {
            IQueryable<Staging> queryS = UnitOfWork.StagingRepository.GetQuery(x => !x.RecordState.Contains("Error"));
            IQueryable<VStagingFieldMappingValue> queryActuals = UnitOfWork.VStagingFieldMappingValueRepository
                .GetQuery(x => x.StagingId.HasValue && x.EpmFieldName == ProjectServerConstants.RecordStateActual);
            IQueryable<VStagingFieldMappingValue> queryGenerals = UnitOfWork.VStagingFieldMappingValueRepository
                .GetQuery(x => x.StagingId.HasValue && x.EpmFieldName == ProjectServerConstants.RecordStateGeneral);
            List<int> staigingsToDelete = (from staging in queryS
                join actuals in queryActuals on staging.StagingId equals actuals.StagingId
                join generals in queryGenerals on staging.StagingId equals generals.StagingId
                where actuals.Value == RecordStateConst.Done && generals.Value == RecordStateConst.Done
                select staging.StagingId).ToList();

            if (staigingsToDelete.Count != 0)
            {
                UnitOfWork.StagingRepository.RemoveRange(x => staigingsToDelete.Contains(x.StagingId));
                UnitOfWork.SaveChanges();
            }
        }

        public List<StagingDTO> GetAllStagings(bool isWorklogs)
        {
            IQueryable<Staging> queryS = UnitOfWork.StagingRepository.GetQuery(x => !x.RecordState.Contains("Error"));
            if (isWorklogs)
            {
                queryS = queryS.Where(x =>  JiraConstants.WorklogEvents.Contains(x.WebHookEvent));
            }
            IQueryable<VStagingFieldMappingValue> queryActuals = UnitOfWork.VStagingFieldMappingValueRepository
                .GetQuery(x => x.StagingId.HasValue && x.EpmFieldName == ProjectServerConstants.RecordStateActual);
            IQueryable<VStagingFieldMappingValue> queryGenerals = UnitOfWork.VStagingFieldMappingValueRepository
                .GetQuery(x => x.StagingId.HasValue && x.EpmFieldName == ProjectServerConstants.RecordStateGeneral);

            IQueryable<StagingDTO> resultQuery = from staging in queryS
                join actuals in queryActuals on staging.StagingId equals actuals.StagingId
                join generals in queryGenerals on staging.StagingId equals generals.StagingId
                select new StagingDTO
                {
                    StagingId = staging.StagingId,
                    SystemId = staging.SystemId,
                    RecordDateCreated = staging.RecordDateCreated,
                    RecordDateUpdated = staging.RecordDateUpdated,
                    WebHookEvent = staging.WebHookEvent,
                    RecordState = staging.RecordState,
                    ChangedFields = staging.ChangedFields,
                    ProjectId = staging.ProjectId,
                    ProjectKey = staging.ProjectKey,
                    ProjectName = staging.ProjectName,
                    IssueId = staging.IssueId,
                    IssueKey = staging.IssueKey,
                    IssueTypeId = staging.IssueTypeId,
                    IssueTypeName = staging.IssueTypeName,
                    IsSubTask = staging.IsSubTask,
                    IssueName = staging.IssueName,
                    ParentEpicId = staging.ParentEpicId,
                    ParentEpicKey = staging.ParentEpicKey,
                    ParentSprintId = staging.ParentSprintId,
                    ParentSprintName = staging.ParentSprintName,
                    ParentVersionId = staging.ParentVersionId,
                    ParentVersionName = staging.ParentVersionName,
                    ParentVersionReleased = staging.ParentVersionReleased,
                    ParentIssueId = staging.ParentIssueId,
                    ParentIssueKey = staging.ParentIssueKey,
                    DateStart = staging.DateStart,
                    DateFinish = staging.DateFinish,
                    Assignee = staging.Assignee,
                    IssueStatus = staging.IssueStatus,
                    DateRelease = staging.DateRelease,
                    DateStartActual = staging.DateStartActual,
                    DateFinishActual = staging.DateFinishActual,
                    IssueActualWork = staging.IssueActualWork,
                    OriginalEstimate = staging.OriginalEstimate,
                    RecordStateActual = actuals.Value,
                    RecordStateGeneral = generals.Value
                };

            if (isWorklogs)
            {
                resultQuery = resultQuery.Where(x => x.RecordStateActual == RecordStateConst.New
                                                     || x.RecordStateActual == RecordStateConst.Pending);
            }
            else
            {
                resultQuery = resultQuery.Where(x => x.RecordStateGeneral == RecordStateConst.New
                                                     || x.RecordStateGeneral == RecordStateConst.Pending);
            }
            List<StagingDTO> stagingsAll = resultQuery.ToList();

            List<int> stagingIdsToDelete = stagingsAll.Where(x => x.RecordStateActual == RecordStateConst.Done
                                                                  && JiraConstants.WorklogEvents.Contains(x.WebHookEvent)
                                                                  && x.WebHookEvent != "syncAll")
                .Select(x => x.StagingId)
                .ToList();

            stagingsAll = stagingsAll.Where(x => !stagingIdsToDelete.Contains(x.StagingId))
                .OrderBy(x => x.ParentIssueKey).ThenBy(x => x.ParentEpicKey).ThenBy(x => x.StagingId).ToList();

            if (stagingIdsToDelete.Count != 0)
            {
                UnitOfWork.StagingRepository.RemoveRange(x => stagingIdsToDelete.Contains(x.StagingId));
                UnitOfWork.SaveChanges();
            }

            //stagingsResult = stagingsResult.OrderBy(x => x.ParentIssueKey).ToList();
            return stagingsAll;
        }

        /// <summary>
        /// SetStagingsRecordState
        /// </summary>
        /// <param name="stagingIds"></param>
        /// <param name="recordType">ProjectServerConstants.RecordStateActual or ProjectServerConstants.RecordStateGeneral</param>
        /// <param name="recordState"></param>
        public void SetStagingsRecordState(List<int> stagingIds, string recordType, string recordState)
        {

            List<int> queryIds = UnitOfWork.VStagingFieldMappingValueRepository
                .GetQuery(x => x.StagingId.HasValue && stagingIds.Contains(x.StagingId.Value)
                               && x.EpmFieldName == recordType && x.StagingFieldMappingValueId.HasValue)
                .Select(x => x.StagingFieldMappingValueId.Value).ToList();
            IQueryable<StagingFieldMappingValue> query = UnitOfWork.StagingFieldMappingValueRepository
                .GetQuery(x => queryIds.Contains(x.StagingFieldMappingValueId));
            if (query.Any())
            {
                query.Update(x => new StagingFieldMappingValue { Value = recordState });
            }
        }

        public void RemoveStagings(List<int> stagingsToDelete, DateTime borderDate)
        {
            UnitOfWork.StagingRepository.RemoveRange(x => stagingsToDelete.Contains(x.StagingId)
                                                          || (borderDate > x.RecordDateCreated && !JiraConstants.WorklogEvents.Contains(x.WebHookEvent)));
            UnitOfWork.SaveChanges();
        }

        public List<VStagingFieldMappingValue> GetCustomValues(List<int> stagingIds)
        {
            return UnitOfWork.VStagingFieldMappingValueRepository
                .GetQuery(x => !x.StagingId.HasValue || stagingIds.Contains(x.StagingId.Value)).ToList();
        }
    }
}