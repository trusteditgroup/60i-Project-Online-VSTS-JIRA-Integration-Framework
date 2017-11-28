using System.Collections.Generic;
using ProjectOnlineSystemConnector.Data.Views.EntityClasses;

namespace ProjectOnlineSystemConnector.DataModel.ViewModel
{
    public class SystemToDbViewModel
    {
        public List<Staging> StagingsToInsert { get; set; }
        public List<Master> MastersToInsert { get; set; }
        public List<Master> MastersToDelete { get; set; }
        public List<MasterWorklog> MasterWorklogsToInsert { get; set; }
        public List<MasterHistory> MasterHistoriesToInsert { get; set; }
        public List<MasterFieldMappingValue> MasterFieldMappingValuesToInsert { get; set; }
        public List<MasterHistoryFieldMappingValue> MasterHistoryFieldMappingValuesToInsert { get; set; }
        public List<StagingFieldMappingValue> StagingFieldMappingValuesToInsert { get; set; }

        public SystemToDbViewModel()
        {
            StagingsToInsert = new List<Staging>();
            MastersToInsert = new List<Master>();
            MastersToDelete = new List<Master>();
            MasterHistoriesToInsert = new List<MasterHistory>();
            MasterFieldMappingValuesToInsert = new List<MasterFieldMappingValue>();
            MasterHistoryFieldMappingValuesToInsert = new List<MasterHistoryFieldMappingValue>();
            StagingFieldMappingValuesToInsert = new List<StagingFieldMappingValue>();
            MasterWorklogsToInsert = new List<MasterWorklog>();
        }
    }
}