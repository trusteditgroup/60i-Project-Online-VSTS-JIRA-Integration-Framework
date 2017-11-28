using System;

namespace ProjectOnlineSystemConnector.DataModel.Common
{
    public class JiraProxyRequest
    {
        public string PostData { get; set; }
        public string RequestType { get; set; }
        public int? SystemId { get; set; }
        public string ApiUrl { get; set; }
        public Guid ProjectUid { get; set; }
        public string ProjectId { get; set; }
        public string EpicKey { get; set; }
    }
}