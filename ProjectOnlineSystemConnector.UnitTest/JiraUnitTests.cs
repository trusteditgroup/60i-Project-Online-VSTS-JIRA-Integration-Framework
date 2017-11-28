using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLog;
using ProjectOnlineSystemConnector.DataAccess.Database.UnitOfWork;
using ProjectOnlineSystemConnector.SyncServices;

namespace ProjectOnlineSystemConnector.UnitTest
{
    [TestClass]
    public class JiraUnitTests : BaseTest
    {
        [TestMethod]
        public async Task SyncAllTestMethod()
        {
            Logger logger = LogManager.GetCurrentClassLogger();
            using (var unitOfWork = new UnitOfWork())
            {
                try
                {
                    ExecuteJira jiraToDbSyncAll = new ExecuteJira(unitOfWork);
                    //await jiraToDbSyncAll.Execute(Guid.Parse("820963c3-b0bc-e711-80d3-00155d748e00"), 1, "11300", null);
                    await jiraToDbSyncAll.Execute(Guid.Parse("62C3E71A-854C-E711-AF35-E4A7A0BDCEBD"), 1, "10400", null);
                }
                catch (Exception exception)
                {
                    logger.Error(exception);
                }
            }
        }
    }
}