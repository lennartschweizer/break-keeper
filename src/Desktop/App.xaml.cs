using System.IO;
using System.Windows;
using System.Windows.Interop;
using Core;

namespace Desktop;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        SessionSnapshot? previewSnapshot = ResolvePreviewSnapshot(e.Args);
        bool isSandbox = previewSnapshot is not null
            || e.Args.Any(arg => string.Equals(arg, "--sandbox", StringComparison.OrdinalIgnoreCase));
        string dataDir = isSandbox
            ? Path.Combine(Path.GetTempPath(), "BreakKeeper-Sandbox", Guid.NewGuid().ToString("N"))
            : AppIdentity.GetAppDataDirectory();
        Directory.CreateDirectory(dataDir);

        if (previewSnapshot is not null)
        {
            var store = new JsonSessionStore(Path.Combine(dataDir, "session.json"));
            store.Save(previewSnapshot);
        }

        string logPath = Path.Combine(dataDir, "startup.log");

        DispatcherUnhandledException += (_, args) =>
        {
            File.AppendAllText(logPath, $"{DateTimeOffset.Now:O} DISPATCHER_ERROR: {args.Exception}{Environment.NewLine}");
        };
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            File.AppendAllText(logPath, $"{DateTimeOffset.Now:O} DOMAIN_ERROR: {args.ExceptionObject}{Environment.NewLine}");
        };

        base.OnStartup(e);

        var window = new MainWindow(dataDir);
        MainWindow = window;
        window.Show();
        window.Activate();

        IntPtr hwnd = new WindowInteropHelper(window).Handle;
        File.AppendAllText(logPath, $"{DateTimeOffset.Now:O} MainWindow shown HWND={hwnd} Visible={window.IsVisible}{Environment.NewLine}");
    }

    private static SessionSnapshot? ResolvePreviewSnapshot(string[] args)
    {
        if (HasArg(args, "--preview-break-complete"))
        {
            return new SessionSnapshot(
                TimerPhase.BreakCompleted,
                TimerPreset.Starter25,
                EntryStep: null,
                PhaseEndUtc: null,
                ExtensionsUsed: 0);
        }

        if (HasArg(args, "--preview-break"))
        {
            return new SessionSnapshot(
                TimerPhase.Break,
                TimerPreset.Starter25,
                EntryStep: null,
                PhaseEndUtc: DateTimeOffset.UtcNow.AddSeconds(20),
                ExtensionsUsed: 0);
        }

        if (HasArg(args, "--preview-prewarning"))
        {
            return new SessionSnapshot(
                TimerPhase.PreWarning,
                TimerPreset.Starter25,
                EntryStep: null,
                PhaseEndUtc: DateTimeOffset.UtcNow.AddMinutes(2),
                ExtensionsUsed: 0);
        }

        return null;
    }

    private static bool HasArg(string[] args, string flag) =>
        args.Any(arg => string.Equals(arg, flag, StringComparison.OrdinalIgnoreCase));
}
