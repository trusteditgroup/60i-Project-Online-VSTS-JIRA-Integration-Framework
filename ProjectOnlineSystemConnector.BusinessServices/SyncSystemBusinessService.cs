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
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using ProjectOnlineSystemConnector.Data.Views.EntityClasses;
using ProjectOnlineSystemConnector.DataAccess.Database.UnitOfWork;
using ProjectOnlineSystemConnector.DataModel.DTO;

namespace ProjectOnlineSystemConnector.BusinessServices
{
    public class SyncSystemBusinessService : BaseBusinessService<SyncSystem, SyncSystemDTO>
    {
        public SyncSystemBusinessService(UnitOfWork unitOfWork) : base(unitOfWork)
        {
        }

        public IQueryable<SyncSystemDTO> GetSyncSystemQuery(Expression<Func<SyncSystem, bool>> whereClause = null)
        {
            IQueryable<SyncSystem> querySs = UnitOfWork.SyncSystemRepository.GetQuery();
            IQueryable<SyncSystemSetting> querySss = UnitOfWork.SyncSystemSettingRepository.GetQuery();
            IQueryable<SyncSystemSettingValue> querySssv = UnitOfWork.SyncSystemSettingValueRepository.GetQuery();
            if (whereClause != null)
            {
                querySs = querySs.Where(whereClause);
            }
            var querySsd = from sss in querySss
                           join sssv in querySssv on sss.SettingId equals sssv.SettingId
                           where sss.Setting == "ActualsStartDate"
                           select new
                           {
                               sssv.SystemId,
                               ActualsStartDateStr = sssv.SettingValue,
                           };
            IQueryable<SyncSystemDTO> syncSystems = from ss in querySs
                                                    join ssd in querySsd on ss.SystemId equals
                                                        ssd.SystemId into ps
                                                    from p in ps.DefaultIfEmpty()
                                                    select new SyncSystemDTO
                                                    {
                                                        SystemId = ss.SystemId,
                                                        DefaultParentTaskName = ss.DefaultParentTaskName,
                                                        SystemApiUrl = ss.SystemApiUrl,
                                                        SystemLogin = ss.SystemLogin,
                                                        SystemName = ss.SystemName,
                                                        SystemPassword = ss.SystemPassword,
                                                        SystemTypeId = ss.SystemTypeId,
                                                        SystemUrl = ss.SystemUrl,
                                                        ActualsStartDateStr = p != null ? p.ActualsStartDateStr : null
                                                    };
            return syncSystems;
        }

        public List<SyncSystemDTO> GetSyncSystemList(Expression<Func<SyncSystem, bool>> whereClause = null)
        {
            IQueryable<SyncSystemDTO> syncSystemsQuery = GetSyncSystemQuery(whereClause);
            List<SyncSystemDTO> syncSystems = syncSystemsQuery.ToList();
            SetSyncSystemActualsStartDate(syncSystems);
            return syncSystems;
        }

        public async Task<List<SyncSystemDTO>> GetSyncSystemListAsync(Expression<Func<SyncSystem, bool>> whereClause = null)
        {
            IQueryable<SyncSystemDTO> syncSystemsQuery = GetSyncSystemQuery(whereClause);
            List<SyncSystemDTO> syncSystems = await syncSystemsQuery.ToListAsync();
            SetSyncSystemActualsStartDate(syncSystems);
            return syncSystems;
        }

        public void SetSyncSystemActualsStartDate(List<SyncSystemDTO> syncSystems)
        {
            foreach (SyncSystemDTO syncSystemDto in syncSystems)
            {
                if (!String.IsNullOrEmpty(syncSystemDto.ActualsStartDateStr))
                {
                    syncSystemDto.ActualsStartDate = DateTime.ParseExact(syncSystemDto.ActualsStartDateStr,
                        "yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture);
                }
            }
        }

        public SyncSystemDTO GetSyncSystem(int systemId)
        {
            return GetSyncSystemList(x => x.SystemId == systemId).FirstOrDefault();
        }

        public async Task<SyncSystemDTO> GetSyncSystemAsync(int systemId)
        {
            List<SyncSystemDTO> syncSystems = await GetSyncSystemListAsync(x => x.SystemId == systemId);
            return syncSystems.FirstOrDefault();
        }
    }
}