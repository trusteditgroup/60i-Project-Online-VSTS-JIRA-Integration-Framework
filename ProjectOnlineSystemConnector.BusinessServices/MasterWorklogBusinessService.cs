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

namespace ProjectOnlineSystemConnector.BusinessServices
{
    public class MasterWorklogBusinessService : BaseBusinessService
    {
        public MasterWorklogBusinessService(UnitOfWork unitOfWork) : base(unitOfWork)
        {
        }

        public List<MasterWorklog> GetActualMasterWorklogs(List<int> systemIds, List<string> issueIds,
            DateTime startDate, DateTime endDate)
        {
            IQueryable<MasterWorklog> query = UnitOfWork.MasterWorklogRepository
                .GetQuery(worklog => issueIds.Contains(worklog.IssueId)
                                     && worklog.DateStarted <= endDate
                                     && worklog.DateStarted >= startDate
                                     && systemIds.Contains(worklog.SystemId));
            List<MasterWorklog> resultList = GetLatestMasterWorklogs(query);
            return resultList;
        }

        public List<MasterWorklog> GetActualMasterWorklogs(List<StagingDTO> stagings, List<SyncSystemDTO> syncSystems,
            DateTime startDateTimesheetPeriods, DateTime endDateTimesheetPeriods)
        {
            IQueryable<MasterWorklog> allWorklogsQuery = null;

            foreach (SyncSystemDTO syncSystemDto in syncSystems)
            {
                DateTime startDate = startDateTimesheetPeriods;
                if (syncSystemDto.ActualsStartDate.HasValue)
                {
                    startDate = startDateTimesheetPeriods >= syncSystemDto.ActualsStartDate.Value
                        ? startDateTimesheetPeriods
                        : syncSystemDto.ActualsStartDate.Value;
                }

                List<string> issueIds = stagings
                    .Where(x => JiraConstants.WorklogEvents.Contains(x.WebHookEvent) && x.SystemId == syncSystemDto.SystemId)
                    .Select(x => x.IssueId)
                    .ToList();

                IQueryable<MasterWorklog> temp = UnitOfWork.MasterWorklogRepository.GetQuery()
                    .Where(x => x.DateStarted.HasValue
                                && x.DateStarted >= startDate
                                && x.SystemId == syncSystemDto.SystemId
                                && issueIds.Contains(x.IssueId) && x.DateStarted <= endDateTimesheetPeriods);
                IQueryable<MasterWorklog> cleanTemp = (from worklog in temp
                                                       group worklog by worklog.WorkLogId into tempGroup
                                                       select new
                                                       {
                                                           tempGroup.Key,
                                                           Worklog = tempGroup.OrderByDescending(x => x.RecordDateUpdated).FirstOrDefault()
                                                       }).Select(x => x.Worklog);

                allWorklogsQuery = allWorklogsQuery?.Union(cleanTemp) ?? cleanTemp;
            }
            if (allWorklogsQuery != null)
            {
                return allWorklogsQuery.ToList();
            }
            return new List<MasterWorklog>();
        }

        public List<MasterWorklog> GetLatestMasterWorklogs(IQueryable<MasterWorklog> query)
        {
            return (from worklog in query
                    group worklog by worklog.WorkLogId
                    into tempGroup
                    select new
                    {
                        tempGroup.Key,
                        Worklog = tempGroup.OrderByDescending(x => x.RecordDateUpdated).FirstOrDefault()
                    }).Select(x => x.Worklog)
                .ToList();
        }

        //public IQueryable<MasterWorklog> GetActualMasterWorklogs(int systemId, DateTime actualsStartDate, 
        //    List<string> issueIds, DateTime startDate, DateTime endDate)
        //{
        //    IQueryable<MasterWorklog> result;
        //    using (var unitOfWork = new UnitOfWork())
        //    {
        //        result = unitOfWork.MasterWorklogRepository.GetQuery()
        //            .Where(x => x.DateStarted.HasValue
        //                        && x.DateStarted >= actualsStartDate
        //                        && x.SystemId == systemId
        //                        && issueIds.Contains(x.IssueId) && x.DateStarted <= endDate
        //                        && x.DateStarted >= startDate);
        //    }
        //    return result;
        //}

        //public async Task CleanMasterWorklogTableDuplicatesAsync(int? systemId = null, List<string> worklogIds = null)
        //{
        //    Logger.Info("CleanMasterWorklogTableDuplicatesAsync START");
        //    Stopwatch stopwatch = new Stopwatch();
        //    stopwatch.Start();
        //    Logger.Info($"CleanMasterWorklogTableDuplicatesAsync stopwatch 1: {stopwatch.ElapsedMilliseconds}");
        //    using (UnitOfWork unitOfWork = new UnitOfWork())
        //    {
        //        IQueryable<MasterWorklog> masterWorklogs = unitOfWork.MasterWorklogRepository.GetQuery();
        //        if (systemId.HasValue)
        //        {
        //            masterWorklogs = masterWorklogs.Where(x => x.SystemId == systemId.Value);
        //        }
        //        if (worklogIds != null && worklogIds.Count != 0)
        //        {
        //            masterWorklogs = masterWorklogs.Where(x => worklogIds.Contains(x.WorkLogId));
        //        }
        //        Logger.Info($"CleanMasterWorklogTableDuplicatesAsync stopwatch 2: {stopwatch.ElapsedMilliseconds}");
        //        var resultGrouping = from masterWorklog in masterWorklogs
        //                             group masterWorklog by new { masterWorklog.SystemId, masterWorklog.WorkLogId } into tempGroup
        //                             select new
        //                             {
        //                                 tempGroup.Key.SystemId,
        //                                 tempGroup.Key.WorkLogId,
        //                                 MasterWorklogs = tempGroup.OrderByDescending(x => x.RecordDateCreated).Skip(1).ToList()
        //                             };
        //        Logger.Info($"CleanMasterWorklogTableDuplicatesAsync stopwatch 3: {stopwatch.ElapsedMilliseconds}");
        //        IOrderedQueryable<MasterWorklog> resultList = (from rg in resultGrouping
        //                                                       where rg.MasterWorklogs.Count != 0
        //                                                       select rg.MasterWorklogs).SelectMany(x => x)
        //                                                       .OrderBy(x => x.MasterWorklogId);

        //        Logger.Info($"CleanMasterWorklogTableDuplicatesAsync stopwatch 4: {stopwatch.ElapsedMilliseconds}");
        //        await unitOfWork.MasterWorklogRepository.RemoveRangeChunkedAsync(resultList);
        //        Logger.Info($"CleanMasterWorklogTableDuplicatesAsync stopwatch 5: {stopwatch.ElapsedMilliseconds}");
        //        //await unitOfWork.SaveChangesAsync();
        //    }
        //    Logger.Info("CleanMasterWorklogTableDuplicatesAsync FINISH");
        //}
        public void RemoveMasterWorklogs(List<int> worklogsToDelete)
        {
            UnitOfWork.MasterWorklogRepository.RemoveRange(x => worklogsToDelete.Contains(x.MasterWorklogId));
        }

        public MasterWorklog GetWorkLogById(int systemId, string worklogId)
        {
            return UnitOfWork.MasterWorklogRepository.GetWorkLogById(systemId, worklogId);
        }
    }
}