using Core;
using Drawing = System.Drawing;
using WinForms = System.Windows.Forms;

namespace Desktop;

internal sealed class TrayIconController : IDisposable
{
    private readonly WinForms.NotifyIcon _notifyIcon;
    private readonly Drawing.Icon? _ownedIcon;
    private string _lastTooltip = string.Empty;

    public TrayIconController(Action onToggleWindow)
    {
        ArgumentNullException.ThrowIfNull(onToggleWindow);

        _ownedIcon = LoadTrayIcon();

        _notifyIcon = new WinForms.NotifyIcon
        {
            Icon = _ownedIcon ?? Drawing.SystemIcons.Application,
            Text = AppIdentity.DisplayName,
            Visible = true
        };

        _notifyIcon.MouseClick += (_, args) =>
        {
            if (args.Button == WinForms.MouseButtons.Left)
                onToggleWindow();
        };
    }

    public void Update(TimerPhase phase, TimeSpan remaining)
    {
        string tooltip = phase switch
        {
            TimerPhase.Focus or TimerPhase.PreWarning => FormatTime(remaining),
            TimerPhase.Break => "Break",
            TimerPhase.BreakCompleted => "Break Complete",
            _ => AppIdentity.DisplayName
        };

        if (tooltip.Length > 63)
            tooltip = tooltip[..63];

        if (!string.Equals(_lastTooltip, tooltip, StringComparison.Ordinal))
        {
            _lastTooltip = tooltip;
            _notifyIcon.Text = tooltip;
        }
    }

    private static Drawing.Icon? LoadTrayIcon()
    {
        var resource = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Assets/tray.ico"));
        if (resource is not null)
        {
            using var stream = resource.Stream;
            return new Drawing.Icon(stream);
        }

        string? exePath = Environment.ProcessPath;
        return !string.IsNullOrWhiteSpace(exePath)
            ? Drawing.Icon.ExtractAssociatedIcon(exePath)
            : null;
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _ownedIcon?.Dispose();
    }

    private static string FormatTime(TimeSpan span)
    {
        int totalMinutes = (int)Math.Floor(Math.Max(0, span.TotalMinutes));
        return $"{totalMinutes:D2}:{Math.Max(0, span.Seconds):D2}";
    }
}
