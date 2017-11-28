using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using ProjectOnlineSystemConnector.Data.Views.EntityClasses;
using ProjectOnlineSystemConnector.DataAccess.Database.Repository.Base;

namespace ProjectOnlineSystemConnector.DataAccess.Database.Repository.Entity
{
    public class MasterHistoryRepository : GenericRepository<MasterHistory>
    {
        public MasterHistoryRepository(DbContext context) : base(context)
        {
        }

        public MasterHistory GetIssueById(int systemId, string id, string issueTypeId)
        {
            return DbSet.FirstOrDefault(x => x.SystemId == systemId && x.IssueId == id && x.IssueTypeId == issueTypeId);
        }

        public List<MasterHistory> GetSprints(int systemId, string parentSprintId)
        {
            return DbSet.Where(x => x.SystemId == systemId && x.ParentSprintId == parentSprintId).ToList();
        }
        public List<MasterHistory> GetIssuesById(int systemId, string id, string issueTypeId)
        {
            return DbSet.Where(x => x.SystemId == systemId && x.IssueId == id && x.IssueTypeId == issueTypeId).ToList();
        }

    }
}