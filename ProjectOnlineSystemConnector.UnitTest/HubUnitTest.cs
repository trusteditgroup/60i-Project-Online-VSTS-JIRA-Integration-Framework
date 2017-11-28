using System;
using System.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLog;
using ProjectOnlineSystemConnector.Common;
using ProjectOnlineSystemConnector.SignalR;

namespace ProjectOnlineSystemConnector.UnitTest
{
    [TestClass]
    public class HubUnitTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            try
            {
                ProjectOnlineSystemConnectorHubHelper.StartHubConnection(ConfigurationManager.AppSettings["SignalRHostUrl"]);
                Logger logger = LogManager.GetCurrentClassLogger();
                logger.LogAndSendMessage(null, "TestMethod1",
                    Guid.NewGuid(), Guid.NewGuid(), 
                    $"TestMethod1 START projectUid: {Guid.NewGuid()}",
                    false, null, CommonConstants.Epm, CommonConstants.End);

            }
            catch (Exception exception)
            {
                var a = 1;
            }
        }
    }
}