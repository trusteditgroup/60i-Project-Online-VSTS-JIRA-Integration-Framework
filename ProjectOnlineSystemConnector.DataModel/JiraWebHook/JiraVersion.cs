using System;

namespace ProjectOnlineSystemConnector.DataModel.JiraWebHook
{
    public class JiraVersion
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int ProjectId { get; set; }
        public bool Released { get; set; }
        public bool Archived { get; set; }
        public DateTime? UserReleaseDate { get; set; }
        public DateTime? ReleaseDate { get; set; }
    }
}