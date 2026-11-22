using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Core;

namespace Desktop;

public partial class MainWindow : Window, IDisposable
{
    private readonly FocusSessionEngine _engine;
    private readonly JsonDurationPreferencesStore _preferencesStore;
    private readonly BreakOverlayCoordinator _overlayCoordinator = new();
    private readonly TrayIconController _trayIcon;
    private readonly DispatcherTimer _timer;
    private bool _isSyncingInputs;
    private TimerPhase? _lastRenderedPhase;
    private PreWarningBannerWindow? _preWarningBanner;
    private bool _preWarningNotificationConsumed;
    private DateTimeOffset _preWarningShownAtUtc;

    public MainWindow() : this(null)
    {
    }

    public MainWindow(string? dataDirectory)
    {
        InitializeComponent();
        DataObject.AddPastingHandler(CustomFocusBox, OnNumericPasting);
        DataObject.AddPastingHandler(CustomBreakBox, OnNumericPasting);
        SourceInitialized += (_, _) => ApplyRoundedWindowCorners();
        Loaded += (_, _) => EnsureIdleFocus();
        Activated += (_, _) => EnsureIdleFocus();
        PreviewKeyDown += OnMainPreviewKeyDown;

        string appDataDir = string.IsNullOrWhiteSpace(dataDirectory)
            ? AppIdentity.GetAppDataDirectory()
            : dataDirectory;
        Directory.CreateDirectory(appDataDir);
        var store = new JsonSessionStore(Path.Combine(appDataDir, "session.json"));
        var auditLog = new FileAuditLog(Path.Combine(appDataDir, "audit.log"));
        _preferencesStore = new JsonDurationPreferencesStore(Path.Combine(appDataDir, "last-durations.json"));

        DurationPreferences? savedDurations = _preferencesStore.Load();
        if (savedDurations is not null)
            ApplyDurationInputs(savedDurations.FocusMinutes, savedDurations.BreakMinutes);

        _engine = new FocusSessionEngine(TimeProvider.System, store, auditLog);
        _engine.RestoreFromStore();
        SyncPresetSelectionFromEngine();
        _trayIcon = new TrayIconController(ToggleWindowFromTray);

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _timer.Tick += (_, _) =>
        {
            _engine.Tick();
            RenderState();
        };
        _timer.Start();

        StateChanged += (_, _) =>
        {
            if (WindowState == WindowState.Minimized)
                HideToTray();
        };

        Closing += (_, e) =>
        {
            if (_engine.Phase == TimerPhase.Break)
            {
                e.Cancel = true;
                return;
            }
            _overlayCoordinator.CloseAll();
            ClosePreWarningBanner();
            _trayIcon.Dispose();
        };

        RenderState();
    }

    private void ToggleWindowFromTray()
    {
        if (WindowState == WindowState.Minimized || !IsVisible)
            RestoreFromTray();
        else
            HideToTray();
    }

    private void HideToTray()
    {
        ShowInTaskbar = false;
        Hide();
    }

    private void RestoreFromTray()
    {
        ShowInTaskbar = true;
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void SyncPresetSelectionFromEngine()
    {
        if (_engine.Phase == TimerPhase.Idle)
            return;

        ApplyDurationInputs(
            (int)Math.Round(_engine.ActivePreset.FocusDuration.TotalMinutes),
            (int)Math.Round(_engine.ActivePreset.BreakDuration.TotalMinutes));
    }

    private void ApplyDurationInputs(int focusMinutes, int breakMinutes)
    {
        _isSyncingInputs = true;
        try
        {
            CustomFocusBox.Text = focusMinutes.ToString(CultureInfo.InvariantCulture);
            CustomBreakBox.Text = breakMinutes.ToString(CultureInfo.InvariantCulture);
        }
        finally
        {
            _isSyncingInputs = false;
        }
    }

    private bool TryBuildSelectedPreset(out TimerPreset? preset, out bool focusValid, out bool breakValid)
    {
        focusValid = TimerPreset.TryParseCustomMinutes(CustomFocusBox.Text, out int focusMinutes);
        breakValid = TimerPreset.TryParseCustomMinutes(CustomBreakBox.Text, out int breakMinutes);
        if (!focusValid || !breakValid)
        {
            preset = null;
            return false;
        }

        if (focusMinutes == 50 && breakMinutes == 10)
        {
            preset = TimerPreset.Deep50;
            return true;
        }

        if (focusMinutes == 25 && breakMinutes == 5)
        {
            preset = TimerPreset.Starter25;
            return true;
        }

        preset = new TimerPreset(
            "Custom",
            TimeSpan.FromMinutes(focusMinutes),
            TimeSpan.FromMinutes(breakMinutes),
            TimeSpan.FromMinutes(2),
            TimeSpan.FromMinutes(5),
            MaxExtensions: 1);
        return true;
    }

    private void RenderState()
    {
        TimerPhase phase = _engine.Phase;
        bool isIdle = phase == TimerPhase.Idle;

        bool hasValidPreset = TryBuildSelectedPreset(out TimerPreset? selectedPreset, out bool focusValid, out bool breakValid);
        TimeSpan displayed = isIdle
            ? (selectedPreset?.FocusDuration ?? TimeSpan.Zero)
            : _engine.Remaining;
        TimerText.Text = isIdle && !hasValidPreset ? "--:--" : FormatTimeSpan(displayed);

        StateBadgeText.Text = phase switch
        {
            TimerPhase.Idle => "IDLE",
            TimerPhase.Focus or TimerPhase.PreWarning => "FOCUS",
            TimerPhase.Break => "BREAK",
            TimerPhase.BreakCompleted => "BREAK COMPLETE",
            _ => "IDLE"
        };

        CustomDurationPanel.Visibility = isIdle ? Visibility.Visible : Visibility.Collapsed;

        StartButton.IsEnabled = isIdle && hasValidPreset;
        StartButton.Visibility = isIdle ? Visibility.Visible : Visibility.Collapsed;

        bool showPreWarning = phase == TimerPhase.PreWarning;
        ExtendButton.Visibility = showPreWarning ? Visibility.Visible : Visibility.Collapsed;
        ExtendButton.IsEnabled = _engine.CanExtend;

        _overlayCoordinator.SyncWithEngine(_engine, RenderState);
        SyncPreWarningBanner(phase, displayed);
        _trayIcon.Update(phase, displayed);

        bool enteredFocus = phase == TimerPhase.Focus && _lastRenderedPhase != TimerPhase.Focus;
        _lastRenderedPhase = phase;
        if (enteredFocus)
            HideToTray();
    }

    private static readonly TimeSpan PreWarningBannerDuration = TimeSpan.FromSeconds(15);

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

    private static string FormatTimeSpan(TimeSpan span)
    {
        int totalMinutes = (int)Math.Floor(span.TotalMinutes);
        return $"{totalMinutes:D2}:{span.Seconds:D2}";
    }

    private void OnCustomMinutesChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (!IsLoaded || _isSyncingInputs || _engine.Phase != TimerPhase.Idle)
            return;

        if (TryBuildSelectedPreset(out TimerPreset? preset, out _, out _) && preset is not null)
        {
            _preferencesStore.Save(new DurationPreferences(
                (int)Math.Round(preset.FocusDuration.TotalMinutes),
                (int)Math.Round(preset.BreakDuration.TotalMinutes)));
        }

        RenderState();
    }

    private static string BuildCandidateText(System.Windows.Controls.TextBox box, string insertedText)
    {
        string current = box.Text ?? string.Empty;
        int start = Math.Clamp(box.SelectionStart, 0, current.Length);
        int length = Math.Clamp(box.SelectionLength, 0, current.Length - start);
        return string.Concat(current.AsSpan(0, start), insertedText, current.AsSpan(start + length));
    }

    private void OnNumericPreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
    {
        if (sender is not System.Windows.Controls.TextBox box)
        {
            e.Handled = true;
            return;
        }

        string candidate = BuildCandidateText(box, e.Text);
        e.Handled = !TimerPreset.CanAcceptDurationInput(candidate);
    }

    private void OnNumericPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Space)
            e.Handled = true;
    }

    private static void OnNumericPasting(object sender, DataObjectPastingEventArgs e)
    {
        if (sender is not System.Windows.Controls.TextBox box
            || !e.DataObject.GetDataPresent(typeof(string))
            || e.DataObject.GetData(typeof(string)) is not string text
            || !TimerPreset.CanAcceptDurationInput(BuildCandidateText(box, text)))
        {
            e.CancelCommand();
        }
    }

    private void OnNumericGotKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
    {
        if (sender is System.Windows.Controls.TextBox box)
            box.SelectAll();
    }

    private void OnNumericPreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is System.Windows.Controls.TextBox box && !box.IsKeyboardFocusWithin)
        {
            box.Focus();
            e.Handled = true;
        }
    }

    private void OnStartClick(object sender, RoutedEventArgs e)
    {
        if (_engine.Phase != TimerPhase.Idle)
            return;

        if (!TryBuildSelectedPreset(out TimerPreset? preset, out _, out _) || preset is null)
            return;

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
        HideToTray();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void EnsureIdleFocus()
    {
        if (_engine.Phase != TimerPhase.Idle)
            return;

        IInputElement? focused = System.Windows.Input.Keyboard.FocusedElement;
        if (focused == CustomFocusBox || focused == CustomBreakBox || focused == StartButton)
            return;

        if (StartButton.IsEnabled)
            StartButton.Focus();
        else
            CustomFocusBox.Focus();
    }

    private void OnMainPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (_engine.Phase == TimerPhase.PreWarning)
        {
            if (e.Key is System.Windows.Input.Key.Up or System.Windows.Input.Key.Down or
                System.Windows.Input.Key.Left or System.Windows.Input.Key.Right)
            {
                if (ExtendButton.IsEnabled)
                    ExtendButton.Focus();
                e.Handled = true;
            }
            return;
        }

        if (_engine.Phase != TimerPhase.Idle)
            return;

        IInputElement? focused = System.Windows.Input.Keyboard.FocusedElement;
        if (focused == CustomFocusBox || focused == CustomBreakBox)
        {
            e.Handled = HandleCustomBoxKeyDown(focused, e.Key);
            return;
        }

        if (focused == StartButton)
        {
            switch (e.Key)
            {
                case System.Windows.Input.Key.Up:
                case System.Windows.Input.Key.Left:
                    CustomFocusBox.Focus();
                    e.Handled = true;
                    break;
                case System.Windows.Input.Key.Right:
                    CustomBreakBox.Focus();
                    e.Handled = true;
                    break;
            }
            return;
        }

        if (e.Key is System.Windows.Input.Key.Up or System.Windows.Input.Key.Down or
            System.Windows.Input.Key.Left or System.Windows.Input.Key.Right)
        {
            EnsureIdleFocus();
            e.Handled = true;
        }
    }

    private bool HandleCustomBoxKeyDown(IInputElement focused, System.Windows.Input.Key key)
    {
        switch (key)
        {
            case System.Windows.Input.Key.Down:
                if (StartButton.IsEnabled)
                    StartButton.Focus();
                return true;
            case System.Windows.Input.Key.Enter:
                if (focused == CustomFocusBox)
                    CustomBreakBox.Focus();
                else
                    StartButton.Focus();
                return true;
            case System.Windows.Input.Key.Left when focused == CustomBreakBox &&
                (CustomBreakBox.CaretIndex == 0 || CustomBreakBox.SelectionLength == CustomBreakBox.Text.Length):
                CustomFocusBox.Focus();
                return true;
            case System.Windows.Input.Key.Right when focused == CustomFocusBox &&
                (CustomFocusBox.CaretIndex >= CustomFocusBox.Text.Length || CustomFocusBox.SelectionLength == CustomFocusBox.Text.Length):
                CustomBreakBox.Focus();
                return true;
            default:
                return false;
        }
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

    public void Dispose()
    {
        _overlayCoordinator.Dispose();
        _trayIcon.Dispose();
        GC.SuppressFinalize(this);
    }
}
