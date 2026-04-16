using System.Runtime.InteropServices;
using VirtualScreen.Core.Protocol;

namespace VirtualScreen.Core;

public static class MonitorHelper
{
    private static readonly IntPtr IDC_ARROW = new(32512);
    private static readonly IntPtr IDC_IBEAM = new(32513);
    private static readonly IntPtr IDC_WAIT = new(32514);
    private static readonly IntPtr IDC_CROSS = new(32515);
    private static readonly IntPtr IDC_SIZENS = new(32645);
    private static readonly IntPtr IDC_SIZEWE = new(32644);
    private static readonly IntPtr IDC_SIZENWSE = new(32642);
    private static readonly IntPtr IDC_SIZENESW = new(32643);
    private static readonly IntPtr IDC_HAND = new(32649);

    private static readonly IntPtr SYS_ARROW;
    private static readonly IntPtr SYS_IBEAM;
    private static readonly IntPtr SYS_WAIT;
    private static readonly IntPtr SYS_CROSS;
    private static readonly IntPtr SYS_SIZENS;
    private static readonly IntPtr SYS_SIZEWE;
    private static readonly IntPtr SYS_SIZENWSE;
    private static readonly IntPtr SYS_SIZENESW;
    private static readonly IntPtr SYS_HAND;

    static MonitorHelper()
    {
        SYS_ARROW = LoadCursor(IntPtr.Zero, IDC_ARROW);
        SYS_IBEAM = LoadCursor(IntPtr.Zero, IDC_IBEAM);
        SYS_WAIT = LoadCursor(IntPtr.Zero, IDC_WAIT);
        SYS_CROSS = LoadCursor(IntPtr.Zero, IDC_CROSS);
        SYS_SIZENS = LoadCursor(IntPtr.Zero, IDC_SIZENS);
        SYS_SIZEWE = LoadCursor(IntPtr.Zero, IDC_SIZEWE);
        SYS_SIZENWSE = LoadCursor(IntPtr.Zero, IDC_SIZENWSE);
        SYS_SIZENESW = LoadCursor(IntPtr.Zero, IDC_SIZENESW);
        SYS_HAND = LoadCursor(IntPtr.Zero, IDC_HAND);
    }

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

    // Cursor position and type
    [DllImport("user32.dll")]
    public static extern bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT { public int X, Y; }

    [DllImport("user32.dll")]
    private static extern bool GetCursorInfo(ref CURSORINFO pci);

    [DllImport("user32.dll")]
    private static extern IntPtr LoadCursor(IntPtr hInstance, IntPtr lpCursorName);

    [StructLayout(LayoutKind.Sequential)]
    private struct CURSORINFO
    {
        public int cbSize;
        public uint flags;
        public IntPtr hCursor;
        public POINT ptScreenPos;
    }

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

    public static (short X, short Y, CursorType Type) GetCursorInfo(int monitorX, int monitorY, int width, int height)
    {
        GetCursorPos(out var p);
        var x = (short)(p.X - monitorX);
        var y = (short)(p.Y - monitorY);

        if (x < 0 || y < 0 || x > width || y > height)
            return (x, y, CursorType.Hidden);

        var ci = new CURSORINFO { cbSize = Marshal.SizeOf<CURSORINFO>() };
        GetCursorInfo(ref ci);

        if ((ci.flags & 1u) == 0)
            return (x, y, CursorType.Hidden);

        var type = ci.hCursor switch
        {
            var h when h == SYS_ARROW => CursorType.Arrow,
            var h when h == SYS_IBEAM => CursorType.IBeam,
            var h when h == SYS_WAIT => CursorType.Wait,
            var h when h == SYS_CROSS => CursorType.Cross,
            var h when h == SYS_SIZENS => CursorType.ResizeNS,
            var h when h == SYS_SIZEWE => CursorType.ResizeEW,
            var h when h == SYS_SIZENWSE => CursorType.ResizeNWSE,
            var h when h == SYS_SIZENESW => CursorType.ResizeNESW,
            var h when h == SYS_HAND => CursorType.Hand,
            _ => CursorType.Arrow
        };

        return (x, y, type);
    }
}