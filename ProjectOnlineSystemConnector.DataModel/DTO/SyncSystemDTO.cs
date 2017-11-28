using System;

namespace ProjectOnlineSystemConnector.DataModel.DTO
{
    public class SyncSystemDTO : IDtoId
    {
        public int SystemId { get; set; }
        public int SystemTypeId { get; set; }
        public string SystemName { get; set; }
        public string SystemUrl { get; set; }
        public string SystemApiUrl { get; set; }
        public string SystemLogin { get; set; }
        public string SystemPassword { get; set; }
        public string DefaultParentTaskName { get; set; }
        public DateTime? ActualsStartDate { get; set; }
        public string ActualsStartDateStr { get; set; }

        public int Id
        {
            get => SystemId;
            set => SystemId = value;
        }

        public Guid __KEY__ { get; set; }
    }
}