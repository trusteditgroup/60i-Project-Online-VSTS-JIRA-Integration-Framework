using System;

namespace ProjectOnlineSystemConnector.DataModel.OData
{
    public class ODataTimesheetPeriod
    {
        public Guid PeriodId { get; set; }
        public string PeriodName { get; set; }
        public string Description { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime StartDate { get; set; }
        public int PeriodStatusId { get; set; }
    }
}