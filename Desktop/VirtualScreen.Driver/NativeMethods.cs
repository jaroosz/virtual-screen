using System.Runtime.InteropServices;

namespace VirtualScreen.Driver;

internal static class NativeMethods
{
    //[DllImport("difxapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    //public static extern bool DiInstallDriverW(
    //    IntPtr hwndParent,
    //    string infPath,
    //    uint flags,
    //    out bool needsReboot);

    //[DllImport("difxapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    //public static extern bool DiUninstallDriverW(
    //    IntPtr hwndParent,
    //    string infPath,
    //    uint flags,
    //    out bool needsReboot);

    //[DllImport("newdev.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    //public static extern bool UpdateDriverForPlugAndPlayDevicesW(
    //IntPtr hwndParent,
    //string hardwareId,
    //string infPath,
    //uint installFlags,
    //out bool needsReboot);

    //[DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    //public static extern bool SetupUninstallOEMInfW(
    //    string infFileName,
    //    uint flags,
    //    IntPtr reserved);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool EnumDisplayDevices(
        string? lpDevice,
        uint iDevNum,
        ref DISPLAY_DEVICE ipDisplayDevice,
        uint dwFlags);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DISPLAY_DEVICE
    {
        public int cb;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceString;
        public uint StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceKey;
    }
}
