using System;

namespace ProjectOnlineSystemConnector.DataModel.OData
{
    public class ODataAssignmentTimephasedDataRecord
    {
        public Guid ProjectId { get; set; }
        public Guid AssignmentId { get; set; }
        public DateTime TimeByDay { get; set; }
        public decimal AssignmentActualWork { get; set; }
        public Guid TaskId { get; set; }
        public Guid ResourceId { get; set; }
    }
}