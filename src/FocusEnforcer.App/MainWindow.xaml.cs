using System.IO;
using System.Windows;
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
    private static readonly SolidColorBrush FocusAccentBrush = Freeze(Color.FromRgb(0x30, 0xD1, 0x58));
    private static readonly SolidColorBrush WarningAccentBrush = Freeze(Color.FromRgb(0xFF, 0x9F, 0x0A));
    private static readonly SolidColorBrush ErrorAccentBrush = Freeze(Color.FromRgb(0xFF, 0x45, 0x3A));

    private readonly FocusSessionEngine _engine;
    private readonly BreakOverlayCoordinator _overlayCoordinator = new();
    private readonly DispatcherTimer _timer;
    private PresetKind _selectedPreset = PresetKind.Deep50;

    public MainWindow()
    {
        InitializeComponent();

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

        (StateBadgeText.Text, StateBadgeText.Foreground) = phase switch
        {
            TimerPhase.Idle => ("IDLE", SecondaryTextBrush),
            TimerPhase.Focus => ("FOCUS", FocusAccentBrush),
            TimerPhase.PreWarning => ("PRE-WARNING", WarningAccentBrush),
            TimerPhase.Break => ("BREAK", ErrorAccentBrush),
            TimerPhase.BreakCompleted => ("BREAK COMPLETE", FocusAccentBrush),
            _ => ("IDLE", SecondaryTextBrush)
        };

        UpdatePresetButton(Preset50Button, _selectedPreset == PresetKind.Deep50, isIdle);
        UpdatePresetButton(Preset25Button, _selectedPreset == PresetKind.Starter25, isIdle);
        UpdatePresetButton(PresetCustomButton, _selectedPreset == PresetKind.Custom, isIdle);

        CustomDurationPanel.Visibility = isIdle && _selectedPreset == PresetKind.Custom
            ? Visibility.Visible
            : Visibility.Hidden;

        EntryStepInputPanel.Visibility = isIdle ? Visibility.Visible : Visibility.Hidden;
        ActiveEntryStepText.Visibility = !isIdle && !string.IsNullOrWhiteSpace(_engine.EntryStep)
            ? Visibility.Visible
            : Visibility.Hidden;
        ActiveEntryStepText.Text = _engine.EntryStep is null ? string.Empty : $"Step: {_engine.EntryStep}";

        StartButton.IsEnabled = isIdle;
        StartButton.Content = phase switch
        {
            TimerPhase.Idle => "Start Focus",
            TimerPhase.Break => "Break Active",
            _ => "Focus Active"
        };

        bool showPreWarning = phase == TimerPhase.PreWarning && _engine.CanExtend;
        PreWarningPanel.Visibility = showPreWarning ? Visibility.Visible : Visibility.Hidden;

        _overlayCoordinator.SyncWithEngine(_engine, RenderState);
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
        _engine.StartFocus(preset, EntryStepBox.Text);
        EntryStepBox.Text = string.Empty;
        RenderState();
    }

    private void OnExtendClick(object sender, RoutedEventArgs e)
    {
        _engine.TryExtendFocus();
        RenderState();
    }
}
