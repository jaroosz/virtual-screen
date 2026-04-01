using System.Runtime.InteropServices;

namespace VirtualScreen.Capture;

internal static class MonitorHelper
{
    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(
        IntPtr hdc,
        IntPtr lprcClip,
        MonitorEnumProc lpfnEnum,
        IntPtr dwData);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfo(
        IntPtr hMonitor,
        ref MONITORINFOEX lpmi);

    private delegate bool MonitorEnumProc(
        IntPtr hMonitor,
        IntPtr hdcMonitor,
        IntPtr lprcMonitor,
        IntPtr dwData);

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

    public record MonitorInfo(string DeviceName, IntPtr HMonitor);

    public static List<MonitorInfo> GetMonitors()
    {
        var result = new List<MonitorInfo>();

        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (hMonitor, _, _, _) =>
        {
            var info = new MONITORINFOEX();
            info.cbSize = Marshal.SizeOf(info);

            if (GetMonitorInfo(hMonitor, ref info))
            {
                Console.WriteLine($"EnumDisplayMonitors: {info.szDevice} | {hMonitor}");
                result.Add(new MonitorInfo(info.szDevice, hMonitor));
            }
            else
            {
                Console.WriteLine($"GetMonitorInfo failed for hMonitor: {hMonitor}");
            }

            return true;
        }, IntPtr.Zero);

        return result;
    }
}