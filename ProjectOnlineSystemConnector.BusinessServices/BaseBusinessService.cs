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
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using NLog;
using ProjectOnlineSystemConnector.DataAccess.Database.UnitOfWork;
using ProjectOnlineSystemConnector.DataModel.DTO;

namespace ProjectOnlineSystemConnector.BusinessServices
{
    public class BaseBusinessService : IDisposable
    {
        protected UnitOfWork UnitOfWork { get; set; }
        protected Logger Logger { get; set; }

        public BaseBusinessService(UnitOfWork unitOfWork)
        {
            UnitOfWork = unitOfWork;
            Logger = LogManager.GetCurrentClassLogger();
        }

        public void Dispose()
        {
            if (UnitOfWork != null)
            {
                UnitOfWork.Dispose();
                UnitOfWork = null;
            }
        }
    }

    public class BaseBusinessService<TEntity, TDTO> : BaseBusinessService where TDTO : IDtoId where TEntity : class
    {
        public BaseBusinessService(UnitOfWork unitOfWork) : base(unitOfWork)
        {
        }

        public virtual async Task<TDTO> GetEntityAsync(int id)
        {
            return await UnitOfWork.GetGenericRepository<TEntity>().GetQuery().ProjectTo<TDTO>()
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public virtual async Task<List<TDTO>> GetListAsync()
        {
            return await UnitOfWork.GetGenericRepository<TEntity>().GetQuery().ProjectTo<TDTO>().ToListAsync();
        }

        public virtual async Task UpdateListAsync(List<TDTO> updateList)
        {
            foreach (TDTO dto in updateList)
            {
                TEntity entity = await UnitOfWork.GetGenericRepository<TEntity>().GetEntityAsync(dto.Id);
                Mapper.Map(dto, entity);
            }
            await UnitOfWork.SaveChangesAsync();
        }

        public virtual async Task<List<TDTO>> InsertListAsync(List<TDTO> insertList)
        {
            List<TEntity> entitesToAdd = insertList.Select(Mapper.Map<TDTO, TEntity>).ToList();
            entitesToAdd = UnitOfWork.GetGenericRepository<TEntity>().AddRange(entitesToAdd);
            await UnitOfWork.SaveChangesAsync();
            insertList = entitesToAdd.Select(Mapper.Map<TEntity, TDTO>).ToList();
            return insertList;
        }

        public virtual async Task RemoveListAsync(List<int> deleteList)
        {
            foreach (int id in deleteList)
                UnitOfWork.GetGenericRepository<TEntity>().Remove(id);
            await UnitOfWork.SaveChangesAsync();
        }
    }
}