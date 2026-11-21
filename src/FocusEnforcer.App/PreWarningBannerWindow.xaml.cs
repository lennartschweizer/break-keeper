using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace FocusEnforcer.App;

public partial class PreWarningBannerWindow : Window
{
    private const int GwlExStyle = -20;
    private const int WsExNoActivate = 0x08000000;

    private readonly Action _onExtend;
    private readonly Action _onDismiss;

    public PreWarningBannerWindow(Action onExtend, Action onDismiss)
    {
        InitializeComponent();
        _onExtend = onExtend;
        _onDismiss = onDismiss;

        SourceInitialized += (_, _) => ApplyNoActivateStyle();
        Loaded += (_, _) => PositionBottomRight();
        SizeChanged += (_, _) => PositionBottomRight();
    }

    public void UpdateState(string formattedRemaining, bool canExtend)
    {
        CountdownText.Text = formattedRemaining;
        ExtendButton.IsEnabled = canExtend;
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
