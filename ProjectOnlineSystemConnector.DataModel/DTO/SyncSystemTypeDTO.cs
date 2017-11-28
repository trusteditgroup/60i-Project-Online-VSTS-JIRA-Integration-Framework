using System;

namespace ProjectOnlineSystemConnector.DataModel.DTO
{
    public class SyncSystemTypeDTO : IDtoId
    {
        public int SystemTypeId { get; set; }
        public string SystemTypeName { get; set; }

        public int Id
        {
            get => SystemTypeId;
            set => SystemTypeId = value;
        }

        public Guid __KEY__ { get; set; }
    }
}