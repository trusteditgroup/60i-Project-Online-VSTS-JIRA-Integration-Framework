using System.Collections.Generic;

namespace ProjectOnlineSystemConnector.DataModel.JiraWebHook
{
    public class JiraChangeLog
    {
        public string Id { get; set; }
        public List<JiraChangeLogItem> Items { get; set; }
    }
}