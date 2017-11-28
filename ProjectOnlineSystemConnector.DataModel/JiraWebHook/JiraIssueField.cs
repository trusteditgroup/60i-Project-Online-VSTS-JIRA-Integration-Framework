using System;
using System.Collections.Generic;

namespace ProjectOnlineSystemConnector.DataModel.JiraWebHook
{
    public class JiraIssueField
    {
        public JiraIssueType IssueType { get; set; }
        public JiraProject Project { get; set; }
        public JiraStatus Status { get; set; }
        public JiraUser Assignee { get; set; }
        public List<JiraVersion> FixVersions { get; set; }
        public JiraIssue Parent { get; set; }
        public JiraWorklog Worklog { get; set; }
        public List<JiraIssueLink> IssueLinks { get; set; }
        public List<JiraComponent> Components { get; set; }
        public string Summary { get; set; }
        public int? TimeOriginalEstimate { get; set; }
        public int? TimeSpent { get; set; }
        public DateTime? Created { get; set; }
    }
}