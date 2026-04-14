using System.Runtime.InteropServices;

namespace VirtualScreen.Core;

public static class MonitorHelper
{
    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

    private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, IntPtr lprcMonitor, IntPtr dwData);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MONITORINFOEX
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int left, top, right, bottom;
    }

    public record MonitorInfo(
        string DeviceName,
        IntPtr HMonitor,
        int X, int Y,
        int Width, int Height,
        bool IsPrimary
    );

    public static List<MonitorInfo> GetMonitors()
    {
        var result = new List<MonitorInfo>();

        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (hMonitor, _, _, _) =>
        {
            var info = new MONITORINFOEX { cbSize = Marshal.SizeOf<MONITORINFOEX>() };

            if (GetMonitorInfo(hMonitor, ref info))
            {
                result.Add(new MonitorInfo(
                    DeviceName: info.szDevice,
                    HMonitor: hMonitor,
                    X: info.rcMonitor.left,
                    Y: info.rcMonitor.top,
                    Width: info.rcMonitor.right - info.rcMonitor.left,
                    Height: info.rcMonitor.bottom - info.rcMonitor.top,
                    IsPrimary: (info.dwFlags & 0x1) != 0
                ));
            }

            return true;
        }, IntPtr.Zero);

        return result;
    }
}