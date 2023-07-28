using NLog;
using System;
using System.Windows;

namespace NecroLink
{
    public partial class App : Application
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public App()
        {
            LogManager.Setup().LoadConfigurationFromFile("NLog.config"); // Load NLog configuration

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Logger.Info("Application started");
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Logger.Fatal(e.ExceptionObject as Exception, "Unhandled exception");
        }
    }
}