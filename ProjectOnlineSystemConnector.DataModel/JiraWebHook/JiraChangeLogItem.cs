namespace ProjectOnlineSystemConnector.DataModel.JiraWebHook
{
    public class JiraChangeLogItem
    {
        public string Field { get; set; }
        public string FieldType { get; set; }
        public string From { get; set; }
        public string FromString { get; set; }
        public string To { get; set; }
        public new string ToString { get; set; }
    }
}