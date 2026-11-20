using System.Runtime.InteropServices;

namespace FocusEnforcer.App;

public readonly record struct MonitorRect(int Left, int Top, int Width, int Height, bool IsPrimary);

public static class MonitorBoundsProvider
{
    private const int MonitorInfoFlagPrimary = 0x00000001;

    private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref NativeRect lprcMonitor, IntPtr dwData);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MonitorInfoEx
    {
        public int Size;
        public NativeRect Monitor;
        public NativeRect Work;
        public int Flags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
    }

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfoEx lpmi);

    public static IReadOnlyList<MonitorRect> GetMonitors()
    {
        var monitors = new List<MonitorRect>();

        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr _, ref NativeRect _, IntPtr _) =>
        {
            var info = new MonitorInfoEx { Size = Marshal.SizeOf<MonitorInfoEx>() };
            if (GetMonitorInfo(hMonitor, ref info))
            {
                monitors.Add(new MonitorRect(
                    info.Monitor.Left,
                    info.Monitor.Top,
                    info.Monitor.Right - info.Monitor.Left,
                    info.Monitor.Bottom - info.Monitor.Top,
                    (info.Flags & MonitorInfoFlagPrimary) != 0));
            }
            return true;
        }, IntPtr.Zero);

        return monitors;
    }
}
