namespace ProjectOnlineSystemConnector.DataModel.Common
{
    public class ProxyResponse
    {
        public string Result { get; set; }
        public object Data { get; set; }
        public string ErrorMessage { get; set; }
        public string ReturnUrl { get; set; }
        public int TotalCount { get; set; }
    }
}