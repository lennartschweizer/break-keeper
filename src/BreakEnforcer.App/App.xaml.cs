using System.IO;
using System.Windows;
using System.Windows.Interop;
using BreakEnforcer.Core;

namespace BreakEnforcer.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        string logDir = AppIdentity.GetAppDataDirectory();
        string logPath = Path.Combine(logDir, "startup.log");

        DispatcherUnhandledException += (_, args) =>
        {
            File.AppendAllText(logPath, $"{DateTimeOffset.Now:O} DISPATCHER_ERROR: {args.Exception}{Environment.NewLine}");
        };
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            File.AppendAllText(logPath, $"{DateTimeOffset.Now:O} DOMAIN_ERROR: {args.ExceptionObject}{Environment.NewLine}");
        };

        base.OnStartup(e);

        var window = new MainWindow();
        MainWindow = window;
        window.Show();
        window.Activate();

        IntPtr hwnd = new WindowInteropHelper(window).Handle;
        File.AppendAllText(logPath, $"{DateTimeOffset.Now:O} MainWindow shown HWND={hwnd} Visible={window.IsVisible}{Environment.NewLine}");
    }
}
