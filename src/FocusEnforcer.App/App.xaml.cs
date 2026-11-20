using System.IO;
using System.Windows;
using System.Windows.Interop;

namespace FocusEnforcer.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        string logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FocusEnforcer");
        Directory.CreateDirectory(logDir);
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
