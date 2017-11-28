using System;

namespace ProjectOnlineSystemConnector.DataModel.DTO
{
    public class SyncSystemSettingDTO : IDtoId
    {
        public int SettingId { get; set; }
        public string Setting { get; set; }

        public int Id
        {
            get => SettingId;
            set => SettingId = value;
        }

        public Guid __KEY__ { get; set; }
    }
}