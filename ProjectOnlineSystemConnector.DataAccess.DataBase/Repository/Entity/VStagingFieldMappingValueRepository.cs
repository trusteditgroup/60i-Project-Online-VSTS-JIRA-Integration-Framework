using System.Data.Entity;
using ProjectOnlineSystemConnector.Data.Views.EntityClasses;
using ProjectOnlineSystemConnector.DataAccess.Database.Repository.Base;

namespace ProjectOnlineSystemConnector.DataAccess.Database.Repository.Entity
{
    public class VStagingFieldMappingValueRepository : GenericRepository<VStagingFieldMappingValue>
    {
        public VStagingFieldMappingValueRepository(DbContext context) : base(context)
        {
        }
    }
}