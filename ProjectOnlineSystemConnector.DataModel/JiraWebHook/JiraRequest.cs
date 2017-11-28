namespace ProjectOnlineSystemConnector.DataModel.JiraWebHook
{
    public class JiraRequest
    {
        public long Timestamp { get; set; }
        public string WebhookEvent { get; set; }
        public string Issue_event_type_name { get; set; }
        public JiraChangeLog ChangeLog { get; set; }
        public JiraIssue Issue { get; set; }
        public JiraSprint Sprint { get; set; }
        public JiraUser User { get; set; }
        public JiraVersion Version { get; set; }
        public JiraWorklog Worklog { get; set; }
    }
}