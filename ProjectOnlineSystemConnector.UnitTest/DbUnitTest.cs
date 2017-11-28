using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProjectOnlineSystemConnector.Data.Views.EntityClasses;
using ProjectOnlineSystemConnector.DataAccess.Database.UnitOfWork;

namespace ProjectOnlineSystemConnector.UnitTest
{
    /// <summary>
    /// Summary description for DbUnitTest
    /// </summary>
    [TestClass]
    public class DbUnitTest
    {

        [TestMethod]
        public void GetSyncSystemListTestMethod()
        {
            UnitOfWork unitOfWork = new UnitOfWork();
            var repo = unitOfWork.GetGenericRepository<SyncSystem>();
            var query = repo.GetQuery();
            List<SyncSystem> list = query.ToList();
            Assert.IsNotNull(list);
        }
    }
}