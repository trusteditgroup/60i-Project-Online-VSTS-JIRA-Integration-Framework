using System;

namespace ProjectOnlineSystemConnector.DataModel.Common
{
    public class LogMessage
    {
        public DateTime StagingRecordDateCreated { get; set; }
        public string StagingIssueKey { get; set; }
        public int StagingSystemId { get; set; }
        public string Action { get; set; }
        public string ActionSource { get; set; }
        public string ActionStartEndMarker { get; set; }
        public string ActionResult { get; set; }
        public Guid ProjectUid { get; set; }
        public Guid WinServiceIterationUid { get; set; }
        public DateTime TimeStampStart { get; set; }
        public DateTime TimeStampEnd { get; set; }
        public double EndStartDiff { get; set; }
        public string Message { get; set; }
        public string ExceptionMessage { get; set; }
        public bool IsBroadcastMessage { get; set; }
        public bool IsWarn { get; set; }
        public int ActionIndex { get; set; }
    }
}