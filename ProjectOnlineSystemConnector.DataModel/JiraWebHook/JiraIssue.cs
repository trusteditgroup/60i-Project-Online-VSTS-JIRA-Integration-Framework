using System;
using System.Collections.Generic;

namespace ProjectOnlineSystemConnector.DataModel.JiraWebHook
{
    public class JiraIssue
    {
        public string Id { get; set; }
        public string Key { get; set; }
        public JiraIssueField Fields { get; set; }
        public Dictionary<string, object> AllFields { get; set; }

        public object GetField(string key)
        {
            if (!String.IsNullOrEmpty(key) && AllFields.ContainsKey(key))
            {
                return AllFields[key];
            }
            return null;
        }
    }
}