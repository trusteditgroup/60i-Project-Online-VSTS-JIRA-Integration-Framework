using System;

namespace ProjectOnlineSystemConnector.DataModel.DTO
{
    public class SyncSystemSettingValueDTO : IDtoId
    {
        public int SyncSystemSettingId { get; set; }
        public int SystemId { get; set; }
        public int SettingId { get; set; }
        public string SettingValue { get; set; }

        public int Id
        {
            get => SyncSystemSettingId;
            set => SyncSystemSettingId = value;
        }

        public Guid __KEY__ { get; set; }
    }
}