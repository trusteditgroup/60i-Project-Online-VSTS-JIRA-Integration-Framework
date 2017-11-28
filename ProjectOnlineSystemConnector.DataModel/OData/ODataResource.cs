using System;

namespace ProjectOnlineSystemConnector.DataModel.OData
{
    public class ODataResource
    {
        public string ResourceNTAccount;
        public Guid ResourceId { get; set; }
        public string ResourceEmailAddress { get; set; }
        public string ResourceName { get; set; }
    }
}