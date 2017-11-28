using System.Collections.Generic;
using Microsoft.ProjectServer.Client;

namespace ProjectOnlineSystemConnector.SyncServices.DataModel.Comparers
{
    public class EnterpriseResourceComparer : IEqualityComparer<EnterpriseResource>
    {
        public bool Equals(EnterpriseResource x, EnterpriseResource y)
        {
            return x.Id == y.Id;
        }

        public int GetHashCode(EnterpriseResource obj)
        {
            return base.GetHashCode();
        }
    }
}