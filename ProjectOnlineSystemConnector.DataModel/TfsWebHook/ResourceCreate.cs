using System.Collections.Generic;

namespace ProjectOnlineSystemConnector.DataModel.TfsWebHook
{
    public class ResourceCreate
    {
        public int Id { get; set; }
        public Dictionary<string, object> Fields { get; set; }
        public List<Relation> Relations { get; set; }
    }
}
