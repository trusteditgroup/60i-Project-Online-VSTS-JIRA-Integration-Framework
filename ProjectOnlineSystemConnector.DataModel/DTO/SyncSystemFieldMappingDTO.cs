using System;

namespace ProjectOnlineSystemConnector.DataModel.DTO
{
    public class SyncSystemFieldMappingDTO : IDtoId
    {
        public int SyncSystemFieldMappingId { get; set; }
        public int SystemId { get; set; }
        public string SystemFieldName { get; set; }
        public string EpmFieldName { get; set; }
        public string FieldType { get; set; }
        public string StagingFieldName { get; set; }
        public bool IsMultiSelect { get; set; }
        public bool IsIdWithValue { get; set; }

        public int Id
        {
            get => SyncSystemFieldMappingId;
            set => SyncSystemFieldMappingId = value;
        }

        public Guid __KEY__ { get; set; }
    }
}