using System.Collections.Generic;
using Atlassian.Jira;

namespace ProjectOnlineSystemConnector.DataAccess.Jira
{
    public static class JiraCache
    {
        public static Dictionary<int, List<CustomField>> SystemCustomFields { get; set; }
        public static List<Project> Projects { get; set; }
    }
}