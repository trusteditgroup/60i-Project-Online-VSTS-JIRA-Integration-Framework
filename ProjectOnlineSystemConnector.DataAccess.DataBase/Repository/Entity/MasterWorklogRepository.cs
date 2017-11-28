using System.Data.Entity;
using System.Linq;
using ProjectOnlineSystemConnector.Data.Views.EntityClasses;
using ProjectOnlineSystemConnector.DataAccess.Database.Repository.Base;

namespace ProjectOnlineSystemConnector.DataAccess.Database.Repository.Entity
{
    public class MasterWorklogRepository : GenericRepository<MasterWorklog>
    {
        public MasterWorklogRepository(DbContext context) : base(context)
        {
        }

        public MasterWorklog GetWorkLogById(int systemId, string id)
        {
            return DbSet.FirstOrDefault(x => x.SystemId == systemId && x.WorkLogId == id);
        }
    }
}