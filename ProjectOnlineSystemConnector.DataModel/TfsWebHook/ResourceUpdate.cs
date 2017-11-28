using System.Collections.Generic;

namespace ProjectOnlineSystemConnector.DataModel.TfsWebHook
{
    public class ResourceUpdate
    {
        public int Id { get; set; }
        public int WorkItemId { get; set; }
        public User RevisedBy { get; set; }
        public Dictionary<string, FieldChangeTrack> Fields { get; set; }
        public Revision Revision { get; set; }
        public UpdateLink _Links { get; set; }
    }
}
