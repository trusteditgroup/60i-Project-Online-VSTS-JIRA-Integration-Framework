using System.Collections.Generic;

namespace ProjectOnlineSystemConnector.DataModel.TfsWebHook
{
    public class Revision
    {
        public string Id { get; set; }
        public Dictionary<string, string> Fields { get; set; }
        public List<Relation> Relations { get; set; }
    }
}
