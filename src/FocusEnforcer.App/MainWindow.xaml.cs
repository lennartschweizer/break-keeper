using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using FocusEnforcer.Core;

namespace FocusEnforcer.App;

public partial class MainWindow : Window
{
    private enum PresetKind { Deep50, Starter25, Custom }

    private static readonly SolidColorBrush WhiteBrush = Freeze(Color.FromRgb(0xFF, 0xFF, 0xFF));
    private static readonly SolidColorBrush BlackBrush = Freeze(Color.FromRgb(0x00, 0x00, 0x00));
    private static readonly SolidColorBrush SurfaceElevatedBrush = Freeze(Color.FromRgb(0x18, 0x18, 0x18));
    private static readonly SolidColorBrush SecondaryTextBrush = Freeze(Color.FromRgb(0x8E, 0x8E, 0x93));

    private readonly FocusSessionEngine _engine;
    private readonly BreakOverlayCoordinator _overlayCoordinator = new();
    private readonly DispatcherTimer _timer;
    private PresetKind _selectedPreset = PresetKind.Deep50;
    private TimerPhase? _lastRenderedPhase;
    private PreWarningBannerWindow? _preWarningBanner;
    private bool _preWarningNotificationConsumed;
    private DateTimeOffset _preWarningShownAtUtc;

    public MainWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => ApplyRoundedWindowCorners();

        string appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FocusEnforcer");
        var store = new JsonSessionStore(Path.Combine(appDataDir, "session.json"));
        var auditLog = new FileAuditLog(Path.Combine(appDataDir, "audit.log"));

        _engine = new FocusSessionEngine(TimeProvider.System, store, auditLog);
        _engine.RestoreFromStore();
        SyncPresetSelectionFromEngine();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _timer.Tick += (_, _) =>
        {
            _engine.Tick();
            RenderState();
        };
        _timer.Start();

        Closing += (_, e) =>
        {
            if (_engine.Phase == TimerPhase.Break)
            {
                e.Cancel = true;
                return;
            }
            _overlayCoordinator.CloseAll();
            ClosePreWarningBanner();
        };

        RenderState();
    }

    private static SolidColorBrush Freeze(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private void SyncPresetSelectionFromEngine()
    {
        if (_engine.Phase == TimerPhase.Idle)
            return;

        _selectedPreset = _engine.ActivePreset.Name switch
        {
            "50/10" => PresetKind.Deep50,
            "25/5" => PresetKind.Starter25,
            _ => PresetKind.Custom
        };
    }

    private TimerPreset BuildSelectedPreset()
    {
        return _selectedPreset switch
        {
            PresetKind.Deep50 => TimerPreset.Deep50,
            PresetKind.Starter25 => TimerPreset.Starter25,
            PresetKind.Custom => new TimerPreset(
                "Custom",
                TimeSpan.FromMinutes(ParseMinutes(CustomFocusBox.Text, fallback: 45)),
                TimeSpan.FromMinutes(ParseMinutes(CustomBreakBox.Text, fallback: 10)),
                TimeSpan.FromMinutes(2),
                TimeSpan.FromMinutes(5),
                MaxExtensions: 1),
            _ => TimerPreset.Deep50
        };
    }

    private static int ParseMinutes(string? text, int fallback)
    {
        return int.TryParse(text, out int value) && value >= 1 && value <= 240 ? value : fallback;
    }

    private void RenderState()
    {
        TimerPhase phase = _engine.Phase;
        bool isIdle = phase == TimerPhase.Idle;

        TimeSpan displayed = isIdle ? BuildSelectedPreset().FocusDuration : _engine.Remaining;
        TimerText.Text = FormatTimeSpan(displayed);

        StateBadgeText.Text = phase switch
        {
            TimerPhase.Idle => "IDLE",
            TimerPhase.Focus or TimerPhase.PreWarning => "FOCUS",
            TimerPhase.Break => "BREAK",
            TimerPhase.BreakCompleted => "BREAK COMPLETE",
            _ => "IDLE"
        };
        StateBadgeText.Foreground = SecondaryTextBrush;

        PresetSelectorPanel.Visibility = isIdle ? Visibility.Visible : Visibility.Collapsed;
        UpdatePresetButton(Preset50Button, _selectedPreset == PresetKind.Deep50, isIdle);
        UpdatePresetButton(Preset25Button, _selectedPreset == PresetKind.Starter25, isIdle);
        UpdatePresetButton(PresetCustomButton, _selectedPreset == PresetKind.Custom, isIdle);

        CustomDurationPanel.Visibility = isIdle && _selectedPreset == PresetKind.Custom
            ? Visibility.Visible
            : Visibility.Collapsed;

        StartButton.IsEnabled = isIdle;
        StartButton.Visibility = isIdle ? Visibility.Visible : Visibility.Collapsed;

        bool showPreWarning = phase == TimerPhase.PreWarning;
        ExtendButton.Visibility = showPreWarning ? Visibility.Visible : Visibility.Collapsed;
        ExtendButton.IsEnabled = _engine.CanExtend;

        _overlayCoordinator.SyncWithEngine(_engine, RenderState);
        SyncPreWarningBanner(phase, displayed);

        bool enteredFocus = phase == TimerPhase.Focus && _lastRenderedPhase != TimerPhase.Focus;
        _lastRenderedPhase = phase;
        if (enteredFocus)
            WindowState = WindowState.Minimized;
    }

    private static readonly TimeSpan PreWarningBannerDuration = TimeSpan.FromSeconds(10);

    private void SyncPreWarningBanner(TimerPhase phase, TimeSpan remaining)
    {
        if (phase != TimerPhase.PreWarning)
        {
            _preWarningNotificationConsumed = false;
            ClosePreWarningBanner();
            return;
        }

        if (!_preWarningNotificationConsumed)
        {
            _preWarningNotificationConsumed = true;
            _preWarningShownAtUtc = DateTimeOffset.UtcNow;
            _preWarningBanner = new PreWarningBannerWindow(
                onExtend: () =>
                {
                    _engine.TryExtendFocus();
                    RenderState();
                },
                onDismiss: ClosePreWarningBanner);
        }

        if (_preWarningBanner is null)
            return;

        if (DateTimeOffset.UtcNow - _preWarningShownAtUtc >= PreWarningBannerDuration)
        {
            ClosePreWarningBanner();
            return;
        }

        int minutesRemaining = (int)Math.Ceiling(remaining.TotalMinutes);
        _preWarningBanner.UpdateState(minutesRemaining, _engine.CanExtend);
        if (!_preWarningBanner.IsVisible)
            _preWarningBanner.Show();
    }

    private void ClosePreWarningBanner()
    {
        if (_preWarningBanner is null)
            return;

        PreWarningBannerWindow banner = _preWarningBanner;
        _preWarningBanner = null;
        banner.FadeOutAndClose();
    }

    private static void UpdatePresetButton(System.Windows.Controls.Button button, bool isSelected, bool isEnabled)
    {
        button.IsEnabled = isEnabled;
        button.Background = isSelected ? WhiteBrush : SurfaceElevatedBrush;
        button.Foreground = isSelected ? BlackBrush : SecondaryTextBrush;
    }

    private static string FormatTimeSpan(TimeSpan span)
    {
        int totalMinutes = (int)Math.Floor(span.TotalMinutes);
        return $"{totalMinutes:D2}:{span.Seconds:D2}";
    }

    private void OnPreset50Click(object sender, RoutedEventArgs e)
    {
        _selectedPreset = PresetKind.Deep50;
        RenderState();
    }

    private void OnPreset25Click(object sender, RoutedEventArgs e)
    {
        _selectedPreset = PresetKind.Starter25;
        RenderState();
    }

    private void OnPresetCustomClick(object sender, RoutedEventArgs e)
    {
        _selectedPreset = PresetKind.Custom;
        RenderState();
    }

    private void OnCustomMinutesChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (IsLoaded && _engine.Phase == TimerPhase.Idle)
            RenderState();
    }

    private void OnStartClick(object sender, RoutedEventArgs e)
    {
        if (_engine.Phase != TimerPhase.Idle)
            return;

        TimerPreset preset = BuildSelectedPreset();
        _engine.StartFocus(preset, null);
        RenderState();
    }

    private void OnExtendClick(object sender, RoutedEventArgs e)
    {
        _engine.TryExtendFocus();
        RenderState();
    }

    private void OnMinimizeClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ApplyRoundedWindowCorners()
    {
        IntPtr hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero)
            return;

        const int dwmwaWindowCornerPreference = 33;
        int preference = 2; // DWMWCP_ROUND
        _ = DwmSetWindowAttribute(hwnd, dwmwaWindowCornerPreference, ref preference, sizeof(int));
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);
}
