namespace ProjectOnlineSystemConnector.DataModel.JiraWebHook
{
    public class JiraIssueType
    {
        public string Id { get; set; }
        public string Description { get; set; }
        public string Name { get; set; }
        public bool Subtask { get; set; }
    }
}