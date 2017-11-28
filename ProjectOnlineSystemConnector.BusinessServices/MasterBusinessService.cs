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

using System.Collections.Generic;
using System.Linq;
using ProjectOnlineSystemConnector.Data.Views.EntityClasses;
using ProjectOnlineSystemConnector.DataAccess.Database.UnitOfWork;

namespace ProjectOnlineSystemConnector.BusinessServices
{
    public class MasterBusinessService : BaseBusinessService
    {
        public MasterBusinessService(UnitOfWork unitOfWork) : base(unitOfWork)
        {
        }

        public List<Master> GetMasters(int systemId, IEnumerable<string> issueIds)
        {
            IQueryable<Master> query = UnitOfWork.MasterRepository.GetQuery()
                .Where(x => x.SystemId == systemId && issueIds.Contains(x.IssueId));
            List<Master> result = GetLatestMasterRecords(query);

            return result;
        }

        public List<Master> GetMasters(IEnumerable<string> issueKeys)
        {
            IQueryable<Master> query = UnitOfWork.MasterRepository.GetQuery()
                .Where(x => issueKeys.Contains(x.IssueKey));
            List<Master> result = GetLatestMasterRecords(query);
            return result;
        }

        public List<Master> GetMasters(List<int> systemIds, IEnumerable<string> issueKeys)
        {
            IQueryable<Master> query = UnitOfWork.MasterRepository.GetQuery()
                .Where(x => issueKeys.Contains(x.IssueKey) && systemIds.Contains(x.SystemId));
            List<Master> result = GetLatestMasterRecords(query);
            return result;
        }

        public List<Master> GetChildMastersForEpic(int systemId, string parentEpicKey)
        {
            IQueryable<Master> query = UnitOfWork.MasterRepository.GetQuery()
                .Where(x => x.SystemId == systemId && x.ParentEpicKey == parentEpicKey);
            List<Master> result = GetLatestMasterRecords(query);
            return result;
        }

        public List<Master> GetChildMastersForEpics(List<int> systemIds, List<string> epicKeys)
        {
            IQueryable<Master> query = UnitOfWork.MasterRepository.GetQuery()
                .Where(x => epicKeys.Contains(x.ParentEpicKey) && systemIds.Contains(x.SystemId));
            List<Master> result = GetLatestMasterRecords(query);

            return result;
        }

        public List<Master> GetChildMastersForIssues(List<int> systemIds, List<string> issueKeys)
        {
            IQueryable<Master> query = UnitOfWork.MasterRepository.GetQuery()
                .Where(x => issueKeys.Contains(x.ParentIssueKey) && systemIds.Contains(x.SystemId));
            List<Master> result = GetLatestMasterRecords(query);
            return result;
        }

        public List<Master> GetLatestMasterRecords(IQueryable<Master> query)
        {
            return (from master in query
                group master by new { master.SystemId, master.IssueKey }
                into tempGroup
                select new
                {
                    tempGroup.Key.SystemId,
                    tempGroup.Key.IssueKey,
                    Master = tempGroup.OrderByDescending(x => x.RecordDateUpdated).FirstOrDefault()
                }).Select(x => x.Master).ToList();
        }

        public Master GetMaster(int systemId, string issueKey)
        {
            Master master = UnitOfWork.MasterRepository.GetQuery().OrderByDescending(x => x.RecordDateCreated)
                .FirstOrDefault(x => x.SystemId == systemId && x.IssueKey == issueKey);
            return master;
        }

        public Master GetIssueById(int systemId, string issueId, string issueTypeId)
        {
            return UnitOfWork.MasterRepository.GetIssueById(systemId, issueId, issueTypeId);
        }

        public int CleanMasterAndMasterWorklog()
        {
            return UnitOfWork.CleanMasterAndMasterWorklog();
        }

        public List<MasterFieldMappingValue> GetMasterFieldMappingValues(int masterId)
        {
            return UnitOfWork.MasterFieldMappingValueRepository.GetQuery(x => x.MasterId == masterId).ToList();
        }
    }
}