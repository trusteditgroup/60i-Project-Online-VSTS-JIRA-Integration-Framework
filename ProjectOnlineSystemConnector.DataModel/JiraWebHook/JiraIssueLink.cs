namespace ProjectOnlineSystemConnector.DataModel.JiraWebHook
{
    public class JiraIssueLink
    {
        public int Id { get; set; }
        public JiraIssueLinkType Type { get; set; }
        public JiraIssue OutwardIssue { get; set; }
        public JiraIssue InwardIssue { get; set; }
    }
}