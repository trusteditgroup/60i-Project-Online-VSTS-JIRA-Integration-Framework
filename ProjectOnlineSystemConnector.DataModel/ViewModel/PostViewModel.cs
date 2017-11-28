using System.Collections.Generic;

namespace ProjectOnlineSystemConnector.DataModel.ViewModel
{
    public class PostViewModel<TDTO>
    {
        public string Marker { get; set; }
        public List<TDTO> InsertList { get; set; }
    }
}