using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using BreakEnforcer.Core;

namespace BreakEnforcer.App;

public partial class BreakOverlayWindow : Window
{
    private static readonly IntPtr HwndTopMost = new(-1);
    private const uint SwpShowWindow = 0x0040;
    private static readonly TimeSpan RequiredHoldDuration = TimeSpan.FromSeconds(5);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint uFlags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    private readonly MonitorRect _bounds;
    private readonly string _expectedPhrase;
    private readonly Action<string, bool> _onEmergencyExitAttempt;
    private readonly Action _onStartNextFocus;
    private readonly Action _onStopToIdle;
    private readonly DispatcherTimer _holdTimer;
    private readonly Stopwatch _holdStopwatch = new();
    private bool _allowClose;
    private bool _isBreakCompleted;

    public BreakOverlayWindow(
        MonitorRect bounds,
        string expectedPhrase,
        Action<string, bool> onEmergencyExitAttempt,
        Action onStartNextFocus,
        Action onStopToIdle)
    {
        InitializeComponent();

        _bounds = bounds;
        _expectedPhrase = expectedPhrase;
        _onEmergencyExitAttempt = onEmergencyExitAttempt;
        _onStartNextFocus = onStartNextFocus;
        _onStopToIdle = onStopToIdle;

        ChallengePhraseText.Text = expectedPhrase;
        CommandManager.AddPreviewExecutedHandler(ChallengeInputBox, OnPreviewCommandExecuted);

        _holdTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _holdTimer.Tick += OnHoldTimerTick;

        SourceInitialized += (_, _) => PositionOnTargetMonitor();
        Loaded += (_, _) =>
        {
            if (_isBreakCompleted && _bounds.IsPrimary)
                BringToForegroundAndFocus();
        };
        PreviewKeyDown += OnOverlayPreviewKeyDown;
        Closing += OnWindowClosing;
    }

    public void UpdateView(bool isBreakCompleted, TimeSpan remaining)
    {
        bool enteredBreakCompleted = isBreakCompleted && !_isBreakCompleted;
        _isBreakCompleted = isBreakCompleted;

        if (isBreakCompleted)
        {
            PhaseTitleText.Text = "BREAK COMPLETE";
            PhaseTitleText.Foreground = System.Windows.Media.Brushes.White;
            CountdownText.Text = "00:00";
            BreakCompleteActions.Visibility = Visibility.Visible;
            EmergencyPanel.Visibility = Visibility.Collapsed;
            ToggleEmergencyButton.Visibility = Visibility.Collapsed;
            ResetHold();
            if (enteredBreakCompleted && _bounds.IsPrimary && IsLoaded)
                BringToForegroundAndFocus();
            return;
        }

        PhaseTitleText.Text = "BREAK";
        PhaseTitleText.Foreground = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(0x8E, 0x8E, 0x93));
        int totalMinutes = (int)Math.Floor(remaining.TotalMinutes);
        CountdownText.Text = $"{totalMinutes:D2}:{remaining.Seconds:D2}";
        BreakCompleteActions.Visibility = Visibility.Collapsed;
        ToggleEmergencyButton.Visibility = Visibility.Visible;
    }

    private void OnStartNextFocusClick(object sender, RoutedEventArgs e) => _onStartNextFocus();

    private void OnStopToIdleClick(object sender, RoutedEventArgs e) => _onStopToIdle();

    private void OnOverlayPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!_isBreakCompleted || e.Key != Key.Enter)
            return;

        if (Keyboard.FocusedElement is System.Windows.Controls.Button focusedButton &&
            focusedButton != StartNextFocusButton)
        {
            return;
        }

        e.Handled = true;
        _onStartNextFocus();
    }

    private void BringToForegroundAndFocus()
    {
        IntPtr hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd != IntPtr.Zero)
        {
            IntPtr foregroundHwnd = GetForegroundWindow();
            uint currentThread = GetCurrentThreadId();
            uint foregroundThread = foregroundHwnd != IntPtr.Zero
                ? GetWindowThreadProcessId(foregroundHwnd, out _)
                : 0;

            if (foregroundThread != 0 && foregroundThread != currentThread)
            {
                AttachThreadInput(foregroundThread, currentThread, true);
                try
                {
                    SetForegroundWindow(hwnd);
                    BringWindowToTop(hwnd);
                }
                finally
                {
                    AttachThreadInput(foregroundThread, currentThread, false);
                }
            }
            else
            {
                SetForegroundWindow(hwnd);
                BringWindowToTop(hwnd);
            }
        }

        Activate();
        StartNextFocusButton.Focus();
    }

    public void ForceClose()
    {
        _holdTimer.Stop();
        _holdStopwatch.Reset();
        _allowClose = true;
        Close();
    }

    public void PositionOnTargetMonitor()
    {
        IntPtr hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero)
            return;

        SetWindowPos(
            hwnd,
            HwndTopMost,
            _bounds.Left,
            _bounds.Top,
            _bounds.Width,
            _bounds.Height,
            SwpShowWindow);
    }

    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (!_allowClose)
            e.Cancel = true;
    }

    private static void OnPreviewCommandExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (e.Command == ApplicationCommands.Paste)
            e.Handled = true;
    }

    private void OnToggleEmergencyClick(object sender, RoutedEventArgs e)
    {
        bool show = EmergencyPanel.Visibility != Visibility.Visible;
        EmergencyPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        if (show)
            ChallengeInputBox.Focus();
    }

    private void OnChallengeInputChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        bool matched = EmergencyExitChallenge.Matches(_expectedPhrase, ChallengeInputBox.Text);
        HoldExitButton.IsEnabled = matched;
        if (!matched)
            ResetHold();
    }

    private void OnHoldStart(object sender, MouseButtonEventArgs e)
    {
        if (!HoldExitButton.IsEnabled)
            return;

        _holdStopwatch.Restart();
        _holdTimer.Start();
    }

    private void OnHoldCancel(object sender, MouseButtonEventArgs e) => ResetHold();

    private void OnHoldLeave(object sender, MouseEventArgs e)
    {
        if (_holdStopwatch.IsRunning)
            ResetHold();
    }

    private void OnHoldTimerTick(object? sender, EventArgs e)
    {
        if (!_holdStopwatch.IsRunning)
            return;

        TimeSpan elapsed = _holdStopwatch.Elapsed;
        if (elapsed >= RequiredHoldDuration)
        {
            _holdTimer.Stop();
            _holdStopwatch.Reset();
            _onEmergencyExitAttempt(ChallengeInputBox.Text, true);
            return;
        }

        double remainingSec = Math.Max(0.1, (RequiredHoldDuration - elapsed).TotalSeconds);
        HoldExitButton.Content = $"Hold {remainingSec:F1}s...";
    }

    private void ResetHold()
    {
        _holdTimer.Stop();
        _holdStopwatch.Reset();
        HoldExitButton.Content = "Hold 5s to Exit";
    }
}
