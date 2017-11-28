using System;

namespace ProjectOnlineSystemConnector.DataModel.OData
{
    public class ODataLookupTableEntry
    {
        public string FullValue { get; set; }
        public Guid Id { get; set; }
        public string InternalName { get; set; }
    }
}