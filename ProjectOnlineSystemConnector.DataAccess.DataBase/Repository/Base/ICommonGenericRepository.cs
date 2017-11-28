using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ProjectOnlineSystemConnector.DataAccess.Database.Repository.Base
{
    /// <summary>
    /// Repository patterns : http://www.asp.net/mvc/overview/older-versions/getting-started-with-ef-5-using-mvc-4/implementing-the-repository-and-unit-of-work-patterns-in-an-asp-net-mvc-application
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public interface ICommonGenericRepository<TEntity> where TEntity : class
    {
        List<TEntity> GetList(Expression<Func<TEntity, bool>> predicate = null);
        IQueryable<TEntity> GetQuery(Expression<Func<TEntity, bool>> predicate = null);
        int GetCount(Expression<Func<TEntity, bool>> predicate = null);
        TEntity GetById(object id);
        void Add(TEntity entity);
        void Remove(object id);
        void Remove(TEntity entityToDelete);
        void RemoveRange(Expression<Func<TEntity, bool>> predicate = null);
        //void DeleteRange(IEnumerable<TEntity> entitiesToDelete);
        int ExecuteSqlCommand(string command, params object[] parameters);
        string GetTableName();
    }
}