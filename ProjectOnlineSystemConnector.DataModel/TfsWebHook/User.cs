using System;

namespace ProjectOnlineSystemConnector.DataModel.TfsWebHook
{
    public class User
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
    }
}
