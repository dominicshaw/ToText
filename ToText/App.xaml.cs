using System;
using System.Windows;
using log4net;

namespace ToText
{
    public partial class App
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(App));

        protected override void OnStartup(StartupEventArgs e)
        {
            Current.DispatcherUnhandledException +=
                (s, ex) => _log.Fatal("Dispatcher Unhandled Exception: {0}", ex.Exception);
            AppDomain.CurrentDomain.UnhandledException +=
                (s, ex) => _log.Fatal($"AppDomain.CurrentDomain Exception: {ex.ExceptionObject}");

            base.OnStartup(e);
        }
    }
}
