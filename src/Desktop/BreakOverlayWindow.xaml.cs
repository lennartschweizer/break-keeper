using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using Core;

namespace Desktop;

public partial class BreakOverlayWindow : Window
{
    private static readonly IntPtr HwndTopMost = new(-1);
    private const uint SwpShowWindow = 0x0040;
    private static readonly TimeSpan RequiredHoldDuration = TimeSpan.FromSeconds(15);

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
    private readonly Action<bool> _onEmergencyExitAttempt;
    private readonly Action _onStartNextFocus;
    private readonly Action _onStopToIdle;
    private readonly DispatcherTimer _holdTimer;
    private readonly Stopwatch _holdStopwatch = new();
    private bool _allowClose;
    private bool _isBreakCompleted;

    public BreakOverlayWindow(
        MonitorRect bounds,
        Action<bool> onEmergencyExitAttempt,
        Action onStartNextFocus,
        Action onStopToIdle)
    {
        InitializeComponent();

        _bounds = bounds;
        _onEmergencyExitAttempt = onEmergencyExitAttempt;
        _onStartNextFocus = onStartNextFocus;
        _onStopToIdle = onStopToIdle;

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
            CountdownText.Text = "00:00";
            BreakCompleteActions.Visibility = Visibility.Visible;
            EmergencyExitButton.Visibility = Visibility.Collapsed;
            ResetHold();
            if (enteredBreakCompleted && _bounds.IsPrimary && IsLoaded)
                BringToForegroundAndFocus();
            return;
        }

        PhaseTitleText.Text = "BREAK";
        int totalMinutes = (int)Math.Floor(remaining.TotalMinutes);
        CountdownText.Text = $"{totalMinutes:D2}:{remaining.Seconds:D2}";
        BreakCompleteActions.Visibility = Visibility.Collapsed;
        EmergencyExitButton.Visibility = Visibility.Visible;
    }

    private void OnStartNextFocusClick(object sender, RoutedEventArgs e) => _onStartNextFocus();

    private void OnStopToIdleClick(object sender, RoutedEventArgs e) => _onStopToIdle();

    private void OnOverlayPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (_isBreakCompleted)
        {
            if (e.Key is Key.Down or Key.Right)
            {
                StopIdleButton.Focus();
                e.Handled = true;
                return;
            }

            if (e.Key is Key.Up or Key.Left)
            {
                StartNextFocusButton.Focus();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                if (Keyboard.FocusedElement == StopIdleButton)
                    _onStopToIdle();
                else
                    _onStartNextFocus();
            }
            return;
        }

        if (e.Key is Key.Up or Key.Down or Key.Left or Key.Right)
        {
            EmergencyExitButton.Focus();
            e.Handled = true;
        }
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

    private void OnHoldStart(object sender, MouseButtonEventArgs e) => BeginHold();

    private void OnHoldCancel(object sender, MouseButtonEventArgs e) => ResetHold();

    private void OnHoldLeave(object sender, MouseEventArgs e)
    {
        if (_holdStopwatch.IsRunning)
            ResetHold();
    }

    private void OnHoldButtonPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Space or Key.Enter)
        {
            e.Handled = true;
            if (!e.IsRepeat && !_holdStopwatch.IsRunning)
                BeginHold();
        }
    }

    private void OnHoldButtonPreviewKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Space or Key.Enter)
        {
            e.Handled = true;
            ResetHold();
        }
    }

    private void OnHoldButtonLostFocus(object sender, RoutedEventArgs e)
    {
        if (_holdStopwatch.IsRunning)
            ResetHold();
    }

    private void BeginHold()
    {
        if (_isBreakCompleted)
            return;

        _holdStopwatch.Restart();
        _holdTimer.Start();
        EmergencyExitButton.Content = $"Hold ({(int)RequiredHoldDuration.TotalSeconds}s)";
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
            _onEmergencyExitAttempt(true);
            return;
        }

        int remainingSec = Math.Max(1, (int)Math.Ceiling((RequiredHoldDuration - elapsed).TotalSeconds));
        EmergencyExitButton.Content = $"Hold ({remainingSec}s)";
    }

    private void ResetHold()
    {
        _holdTimer.Stop();
        _holdStopwatch.Reset();
        EmergencyExitButton.Content = "Emergency Exit (Hold)";
    }
}
