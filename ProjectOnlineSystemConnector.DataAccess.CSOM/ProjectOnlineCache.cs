using System.Collections.Generic;
using ProjectOnlineSystemConnector.DataModel.OData;

namespace ProjectOnlineSystemConnector.DataAccess.CSOM
{
    public static class ProjectOnlineCache
    {
        //public static List<CustomField> CustomFields { get; set; }
        //public static Dictionary<string, string> CustomFieldsDict { get; set; }
        //public static List<EnterpriseResource> EnterpriseResources { get; set; }
        //public static List<ODataTimesheetPeriod> TimesheetPeriods { get; set; }

        public static List<ODataResource> ODataResources { get; set; }
        public static List<ODataTimesheetPeriod> TimesheetPeriods { get; set; }
        public static Dictionary<string, string> CustomFieldsDict { get; set; }
        public static ODataResource CurrentODataResource { get; set; }
        public static bool NeedInitCache()
        {
            return CustomFieldsDict == null || ODataResources == null || TimesheetPeriods == null;
        }
    }
}