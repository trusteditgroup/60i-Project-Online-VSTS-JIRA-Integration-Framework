using System;
using System.ServiceProcess;
using NLog;

namespace ProjectOnlineSystemConnector.WinService
{
    static class Program
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;

            var servicesToRun = new ServiceBase[]
            {
                new ProjectOnlineSystemConnectorWinService()
            };
            ServiceBase.Run(servicesToRun);
        }

        private static void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            logger.Fatal((Exception)e.ExceptionObject);
        }
    }
}