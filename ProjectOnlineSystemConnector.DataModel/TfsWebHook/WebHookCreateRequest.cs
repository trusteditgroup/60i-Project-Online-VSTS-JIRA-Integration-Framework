namespace ProjectOnlineSystemConnector.DataModel.TfsWebHook
{
    public class WebHookCreateRequest
    {
        public string EventType { get; set; }
        public Message Message { get; set; }
        public ResourceCreate Resource { get; set; }
    }
}
