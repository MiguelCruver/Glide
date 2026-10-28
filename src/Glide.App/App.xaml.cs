using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace Glide.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        AppDomain.CurrentDomain.UnhandledException += (s, ev) =>
        {
            File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_crash.log"), ev.ExceptionObject.ToString());
        };

        DispatcherUnhandledException += (s, ev) =>
        {
            File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dispatcher_crash.log"), ev.Exception.ToString());
        };

        base.OnStartup(e);
    }
}
