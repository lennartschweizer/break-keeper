using System.Diagnostics;
using System.Runtime.InteropServices;
using Core;

namespace Desktop;

public sealed class BreakKeyboardHook : IDisposable
{
    private const int WhKeyboardLl = 13;
    private const int WmKeyDown = 0x0100;
    private const int WmKeyUp = 0x0101;
    private const int WmSysKeyDown = 0x0104;
    private const int WmSysKeyUp = 0x0105;
    private const int VkControl = 0x11;
    private const uint LlkhfAltDown = 0x20;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct KbdLlHookStruct
    {
        public int vkCode;
        public int scanCode;
        public int flags;
        public int time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    private readonly LowLevelKeyboardProc _proc;
    private IntPtr _hookHandle;

    public BreakKeyboardHook()
    {
        _proc = HookCallback;
    }

    public bool IsInstalled => _hookHandle != IntPtr.Zero;

    public void Install()
    {
        if (_hookHandle != IntPtr.Zero)
            return;

        using Process currentProcess = Process.GetCurrentProcess();
        using ProcessModule? module = currentProcess.MainModule;
        IntPtr moduleHandle = GetModuleHandle(module?.ModuleName);
        _hookHandle = SetWindowsHookEx(WhKeyboardLl, _proc, moduleHandle, 0);
    }

    public void Uninstall()
    {
        if (_hookHandle == IntPtr.Zero)
            return;

        UnhookWindowsHookEx(_hookHandle);
        _hookHandle = IntPtr.Zero;
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msg = wParam.ToInt32();
            if (msg == WmKeyDown || msg == WmSysKeyDown || msg == WmKeyUp || msg == WmSysKeyUp)
            {
                KbdLlHookStruct info = Marshal.PtrToStructure<KbdLlHookStruct>(lParam);
                bool altDown = (info.flags & LlkhfAltDown) != 0;
                bool ctrlDown = (GetAsyncKeyState(VkControl) & 0x8000) != 0;

                if (BreakKeyFilter.ShouldBlock(info.vkCode, altDown, ctrlDown))
                    return (IntPtr)1;
            }
        }

        return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    public void Dispose() => Uninstall();
}
