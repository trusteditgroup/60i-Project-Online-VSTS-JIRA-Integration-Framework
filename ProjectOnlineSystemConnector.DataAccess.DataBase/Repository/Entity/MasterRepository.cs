using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using ProjectOnlineSystemConnector.Data.Views.EntityClasses;
using ProjectOnlineSystemConnector.DataAccess.Database.Repository.Base;

namespace ProjectOnlineSystemConnector.DataAccess.Database.Repository.Entity
{
    public class MasterRepository : GenericRepository<Master>
    {
        public MasterRepository(DbContext context) : base(context)
        {
        }

        public Master GetIssueById(int systemId, string id, string issueTypeId)
        {
            return DbSet.OrderByDescending(x => x.RecordDateUpdated)
                .FirstOrDefault(x => x.SystemId == systemId && x.IssueId == id && x.IssueTypeId == issueTypeId);
        }

        public List<Master> GetSprints(int systemId, string parentSprintId)
        {
            return DbSet.Where(x => x.SystemId == systemId && x.ParentSprintId == parentSprintId).ToList();
        }
    }
}