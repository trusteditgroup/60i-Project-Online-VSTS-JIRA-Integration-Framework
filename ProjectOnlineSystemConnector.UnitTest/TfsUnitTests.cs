using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProjectOnlineSystemConnector.DataAccess.Database.UnitOfWork;
using ProjectOnlineSystemConnector.DataModel.Common;
using ProjectOnlineSystemConnector.SyncServices;

namespace ProjectOnlineSystemConnector.UnitTest
{
    [TestClass]
    public class TfsUnitTests
    {
        [TestMethod]
        public async Task TestMethod1()
        {
            try
            {
                VssConnection connection =
                    new VssConnection(new Uri("https://project-online.visualstudio.com/DefaultCollection"),
                        new VssBasicCredential(String.Empty, "h6u6pnyfkxpazygcxqlowcx6ehczu5mfmw2xx776gwo6yenfm5da"));
                WorkItemTrackingHttpClient witClient = connection.GetClient<WorkItemTrackingHttpClient>();
                WorkItem workItem = await witClient.GetWorkItemAsync(225, expand: WorkItemExpand.Relations);
                Assert.IsNotNull(workItem);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        [TestMethod]
        public async Task ExecuteTestMethod()
        {
            using (var unitOfWork = new UnitOfWork())
            {
                ExecuteTfs syncAll = new ExecuteTfs(unitOfWork);

                ProxyResponse res = await syncAll.Execute(2, 275, null);
                Assert.AreEqual(res.Result, "ok");
            }
        }
    }
}