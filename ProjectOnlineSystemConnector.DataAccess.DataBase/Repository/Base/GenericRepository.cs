using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NLog;
using Z.EntityFramework.Plus;

namespace ProjectOnlineSystemConnector.DataAccess.Database.Repository.Base
{
    /// <inheritdoc />
    /// Base class for repositories. More about Repository patterns : http://www.asp.net/mvc/overview/older-versions/getting-started-with-ef-5-using-mvc-4/implementing-the-repository-and-unit-of-work-patterns-in-an-asp-net-mvc-application
    /// <typeparam name="TEntity"></typeparam>
    public class GenericRepository<TEntity> : ICommonGenericRepository<TEntity> where TEntity : class
    {
        protected readonly DbContext Db;
        protected readonly DbSet<TEntity> DbSet;
        private readonly Logger logger = LogManager.GetCurrentClassLogger();

        public GenericRepository(DbContext context)
        {
            Db = context;
            DbSet = Db.Set<TEntity>();
        }

        public virtual List<TEntity> GetList(Expression<Func<TEntity, bool>> predicate = null)
        {
            return GetQuery(predicate).ToList();
        }

        public virtual IQueryable<TEntity> GetQuery(Expression<Func<TEntity, bool>> predicate = null)
        {
            return predicate == null ? DbSet : DbSet.Where(predicate);
        }

        public int GetCount(Expression<Func<TEntity, bool>> predicate = null)
        {
            return GetQuery(predicate).Count();
        }

        public async Task<int> GetCountAsync(Expression<Func<TEntity, bool>> predicate = null)
        {
            return await GetQuery(predicate).CountAsync();
        }

        public virtual async Task<TEntity> GetEntityAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await DbSet.FirstOrDefaultAsync(predicate);
        }

        public virtual async Task<TEntity> GetEntityAsync(object id)
        {
            return await DbSet.FindAsync(id);
        }

        public virtual TEntity GetById(object id)
        {
            return DbSet.Find(id);
        }

        public virtual async Task<TEntity> GetByIdAsync(object id)
        {
            return await DbSet.FindAsync(id);
        }

        public virtual void Add(TEntity entity)
        {
            DbSet.Add(entity);
        }

        public virtual List<TEntity> AddRange(IEnumerable<TEntity> entities)
        {
            return DbSet.AddRange(entities).ToList();
        }

        public virtual async Task<List<TEntity>> AddRangeChunkedAsync(IList<TEntity> entities)
        {
            logger.Info($"AddRangeChunkedAsync entities: {entities.Count}");
            List<TEntity> insertedIssues = new List<TEntity>();
            int skip = 0;
            int take = 1000;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            while (true)
            {
                logger.Info(
                    $"AddRangeChunkedAsync 1 skip: {skip}; take: {take}; stopwatch: {stopwatch.ElapsedMilliseconds}");
                List<TEntity> tempCollection = entities.Skip(skip).Take(take).ToList();
                if (tempCollection.Count == 0)
                {
                    break;
                }
                insertedIssues.AddRange(AddRange(tempCollection));
                logger.Info(
                    $"AddRangeChunkedAsync 2 skip: {skip}; take: {take}; stopwatch: {stopwatch.ElapsedMilliseconds}");
                await Db.SaveChangesAsync();
                logger.Info(
                    $"AddRangeChunkedAsync 3 skip: {skip}; take: {take}; stopwatch: {stopwatch.ElapsedMilliseconds}");
                skip += take;
            }
            logger.Info($"AddRangeChunkedAsync insertedIssues: {insertedIssues.Count}");
            return insertedIssues;
        }

        public virtual void AddOrUpdate(TEntity[] entities)
        {
            DbSet.AddOrUpdate(entities);
        }

        public virtual void Remove(object id)
        {
            TEntity entityToDelete = DbSet.Find(id);
            if (entityToDelete != null)
            {
                Remove(entityToDelete);
            }
        }

        public virtual void Remove(TEntity entityToDelete)
        {
            if (Db.Entry(entityToDelete).State == EntityState.Detached)
            {
                DbSet.Attach(entityToDelete);
            }
            DbSet.Remove(entityToDelete);
        }

        public List<TEntity> RemoveRange(List<TEntity> entitiesToDelete)
        {
            if (entitiesToDelete.Count == 0)
            {
                return entitiesToDelete;
            }
            return DbSet.RemoveRange(entitiesToDelete).ToList();
        }

        //public virtual async Task RemoveRangeChunkedAsync(IOrderedQueryable<TEntity> entitiesToDelete)
        //{
        //    int skip = 0;
        //    int take = 1000;
        //    Stopwatch stopwatch = new Stopwatch();
        //    stopwatch.Start();
        //    logger.Info($"RemoveRangeChunkedAsync entitiesToDelete: {entitiesToDelete.Count()}");
        //    //List<TEntity> resultCollection = entitiesToDelete.ToList();
        //    while (true)
        //    {
        //        logger.Info(
        //            $"RemoveRangeChunkedAsync 1 skip: {skip}; take: {take}; stopwatch: {stopwatch.ElapsedMilliseconds}");
        //        IQueryable<TEntity> tempCollection = entitiesToDelete.Skip(skip).Take(take);
        //        logger.Info(
        //            $"RemoveRangeChunkedAsync 2 skip: {skip}; take: {take}; tempCollection: {tempCollection.Count()}; stopwatch: {stopwatch.ElapsedMilliseconds}");
        //        if (tempCollection.Count() < 999)
        //        {
        //            break;
        //        }
        //        logger.Info(
        //            $"RemoveRangeChunkedAsync 3 skip: {skip}; take: {take}; stopwatch: {stopwatch.ElapsedMilliseconds}");
        //        DbSet.RemoveRange(tempCollection);
        //        int count = await Db.SaveChangesAsync();
        //        logger.Info(
        //            $"RemoveRangeChunkedAsync 4 skip: {skip}; take: {take}; stopwatch: {stopwatch.ElapsedMilliseconds}");
        //        //skip += take;
        //        await Task.Delay(5 * 1000);
        //    }
        //}

        public virtual void RemoveRange(Expression<Func<TEntity, bool>> predicate = null)
        {
            if (predicate != null)
            {
                IQueryable<TEntity> query = DbSet.Where(predicate);
                if (query.Any())
                {
                    query.Delete();
                }
            }
            else
            {
                DbSet.Delete();
            }
        }

        public virtual int ExecuteSqlCommand(string command, params object[] parameters)
        {
            return Db.Database.ExecuteSqlCommand(command, parameters);
        }

        public string GetTableName()
        {
            ObjectContext objectContext = ((IObjectContextAdapter)Db).ObjectContext;
            return GetTableName(objectContext);
        }

        private static string GetTableName(ObjectContext objectContext)
        {
            string sql = objectContext.CreateObjectSet<TEntity>().ToTraceString();
            Regex regex = new Regex("FROM (?<table>.*) AS");
            Match match = regex.Match(sql);

            string table = match.Groups["table"].Value;
            return table;
        }
    }
}