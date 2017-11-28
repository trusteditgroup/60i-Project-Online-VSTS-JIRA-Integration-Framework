using System;
using System.Collections.Generic;

namespace ProjectOnlineSystemConnector.DataModel.OData
{
    public class ODataLookupTable
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public List<ODataLookupTableEntry> Entries { get; set; }
    }
}