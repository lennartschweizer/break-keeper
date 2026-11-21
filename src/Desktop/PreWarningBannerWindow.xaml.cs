using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Animation;

namespace Desktop;

public partial class PreWarningBannerWindow : Window
{
    private const int GwlExStyle = -20;
    private const int WsExNoActivate = 0x08000000;
    private static readonly TimeSpan FadeDuration = TimeSpan.FromMilliseconds(350);

    private readonly Action _onExtend;
    private readonly Action _onDismiss;
    private bool _isFadingOut;

    public PreWarningBannerWindow(Action onExtend, Action onDismiss)
    {
        InitializeComponent();
        _onExtend = onExtend;
        _onDismiss = onDismiss;

        SourceInitialized += (_, _) => ApplyNoActivateStyle();
        Loaded += (_, _) =>
        {
            PositionBottomRight();
            FadeIn();
        };
        SizeChanged += (_, _) => PositionBottomRight();
    }

    public void UpdateState(int minutesRemaining, bool canExtend)
    {
        MessageText.Text = $"Break in {Math.Max(1, minutesRemaining)} min";
        ExtendButton.IsEnabled = canExtend;
    }

    public void FadeOutAndClose()
    {
        if (_isFadingOut)
            return;

        _isFadingOut = true;
        var fadeOut = new DoubleAnimation(Opacity, 0.0, FadeDuration)
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };
        fadeOut.Completed += (_, _) => Close();
        BeginAnimation(OpacityProperty, fadeOut);
    }

    private void FadeIn()
    {
        var fadeIn = new DoubleAnimation(0.0, 1.0, FadeDuration)
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        BeginAnimation(OpacityProperty, fadeIn);
    }

    private void ApplyNoActivateStyle()
    {
        IntPtr hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero)
            return;

        int exStyle = GetWindowLong(hwnd, GwlExStyle);
        SetWindowLong(hwnd, GwlExStyle, exStyle | WsExNoActivate);
    }

    private void PositionBottomRight()
    {
        Rect workArea = SystemParameters.WorkArea;
        Left = workArea.Right - ActualWidth - 16;
        Top = workArea.Bottom - ActualHeight - 16;
    }

    private void OnExtendClick(object sender, RoutedEventArgs e)
    {
        _onExtend();
    }

    private void OnDismissClick(object sender, RoutedEventArgs e)
    {
        _onDismiss();
    }

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
}
