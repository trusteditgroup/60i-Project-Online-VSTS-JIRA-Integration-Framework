using System;

namespace ProjectOnlineSystemConnector.DataModel.JiraWebHook
{
    public class JiraWorklog
    {
        public JiraUser Author { get; set; }
        public string Comment { get; set; }
        public string Started { get; set; }
        public string Updated { get; set; }
        public string Created { get; set; }
        public long TimeSpentSeconds { get; set; }
        public string Id { get; set; }
        public string Self { get; set; }

        public string IssueId
        {
            get
            {
                if (String.IsNullOrEmpty(Self))
                {
                    return null;
                }
                string[] parts =  Self.Split(new[] {"/"}, StringSplitOptions.RemoveEmptyEntries);
                int index = parts.Length - 3;
                return parts[index];
            }
        }
    }
}