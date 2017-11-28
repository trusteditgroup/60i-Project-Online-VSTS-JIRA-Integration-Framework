using System.Collections.Generic;

namespace ProjectOnlineSystemConnector.Common
{
    public static class JiraConstants
    {
        public static List<string> WorklogEvents = new List<string>
        {
            "jira:worklog_created",
            "jira:worklog_updated",
            "jira:worklog_deleted",
            "syncAll"
        };

        public static string EpicNameFieldNameLowerCase = EpicNameFieldName.ToLower();
        public static string EpicLinkFieldNameLowerCase = EpicLinkFieldName.ToLower();
        public static string SprintFieldNameLowerCase = SprintFieldName.ToLower();

        public const string EpicNameFieldName = "Epic Name";
        public const string EpicLinkFieldName = "Epic Link";
        public const string SprintFieldName = "Sprint";
    }
}