using System;
using System.Threading.Tasks;
using ProjectOnlineSystemConnector.Data.Views;
using ProjectOnlineSystemConnector.Data.Views.EntityClasses;

namespace ProjectOnlineSystemConnector.DataAccess.Database.UnitOfWork
{
    public partial class UnitOfWork : IDisposable
    {
        public ProjectOnlineSystemConnectorDataViewsEntities Context { get; private set; }

        public UnitOfWork()
        {
            Context = new ProjectOnlineSystemConnectorDataViewsEntities();
            Context.Database.CommandTimeout = Int32.MaxValue;
        }

        public void Dispose()
        {
            if (Context != null)
            {
                Context.Dispose();
                Context = null;
            }
        }

        //public DbContextTransaction BeginTransaction()
        //{
        //    AzureDbConfiguration.SuspendExecutionStrategy = true;
        //    return Context.Database.BeginTransaction();
        //}
        //public DbContextTransaction BeginTransaction(IsolationLevel isolationLevel)
        //{
        //    AzureDbConfiguration.SuspendExecutionStrategy = true;
        //    return Context.Database.BeginTransaction();
        //}

        //public void CommitTransaction(DbContextTransaction trans)
        //{
        //    trans.Commit();
        //    AzureDbConfiguration.SuspendExecutionStrategy = false;
        //    trans.Dispose();
        //}

        //public void RollbackTransaction(DbContextTransaction trans)
        //{
        //    if (trans != null)
        //    {
        //        trans.Rollback();
        //        AzureDbConfiguration.SuspendExecutionStrategy = false;
        //        trans.Dispose();
        //    }
        //}

        public int SaveChanges()
        {
            return Context.SaveChanges();
        }

        public async Task<int> SaveChangesAsync()
        {
            return await Context.SaveChangesAsync();
        }
    }
}