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
using ProjectOnlineSystemConnector.DataModel.DTO;

namespace ProjectOnlineSystemConnector.BusinessServices
{
    public class SyncSystemFieldMappingBusinessService : BaseBusinessService<SyncSystemFieldMapping, SyncSystemFieldMappingDTO>
    {
        public SyncSystemFieldMappingBusinessService(UnitOfWork unitOfWork) : base(unitOfWork)
        {
        }

        public List<SyncSystemFieldMapping> GetSyncSystemFieldMappings(int systemId)
        {
            return UnitOfWork.SyncSystemFieldMappingRepository
                .GetQuery(x => x.SystemId == systemId).ToList();
        }
    }
}