namespace ProjectOnlineSystemConnector.DataModel.TfsWebHook
{
    public class WebHookUpdateRequest
    {
        public string EventType { get; set; }
        public Message Message { get; set; }
        public ResourceUpdate Resource { get; set; }
    }
}
