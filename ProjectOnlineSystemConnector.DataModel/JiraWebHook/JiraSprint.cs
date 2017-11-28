namespace ProjectOnlineSystemConnector.DataModel.JiraWebHook
{
    public class JiraSprint
    {
        /// <summary>
        /// Do not use it for setting or comparing Task Custom field
        /// </summary>
        //public int Id { get; set; }
        //public string IdStr => Id.ToString();
        public string Id { get; set; }
        public string Name { get; set; }
        public string State { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string CompleteDate { get; set; }
    }
}