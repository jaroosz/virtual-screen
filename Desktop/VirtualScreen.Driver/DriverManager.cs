using System.Diagnostics;
using System.Runtime.InteropServices;
using VirtualScreen.Core;

namespace VirtualScreen.Driver;

public class DriverManager : IDriverManager
{
    private readonly string _driverPath;
    private const string DeviceInstanceId = "ROOT\\DISPLAY\\0000";

    public DriverManager(string driverPath)
    {
        _driverPath = driverPath;
    }

    // driver installation
    public bool IsDriverInstalled() =>
        GetVirtualDevice() != null;

    public bool InstallDriver()
    {
        var infPath = Path.Combine(_driverPath, "MttVDD.inf");
        if (!File.Exists(infPath))
            return false;

        var code = RunPnpUtil($"/add-driver \"{infPath}\" /install");
        RunPnpUtil("/scan-devices");
        return code is 0 or 259;
    }

    public bool UninstallDriver() => RunPnpUtil("/delete-driver MttVDD.inf /uninstall") == 0;

    // enable/disable driver
    public bool IsDriverEnabled() => GetVirtualDevice() is { } d && (d.StateFlags & 0x1) != 0;

    public bool EnableDriver() => RunPnpUtil($"/enable-device \"{DeviceInstanceId}\"") is 0 or 259;

    public bool DisableDriver() => RunPnpUtil($"/disable-device \"{DeviceInstanceId}\"") is 0 or 259;

    // monitor
    public string? GetVirtualMonitorDeviceName() => GetVirtualDevice()?.DeviceName;

    private NativeMethods.DISPLAY_DEVICE? GetVirtualDevice()
    {
        return GetAllDisplayDevices()
            .Cast<NativeMethods.DISPLAY_DEVICE?>()
            .FirstOrDefault(d => d!.Value.DeviceID.Contains("MttVDD", StringComparison.OrdinalIgnoreCase));
    }

    private List<NativeMethods.DISPLAY_DEVICE> GetAllDisplayDevices()
    {
        var result = new List<NativeMethods.DISPLAY_DEVICE>();
        var device = new NativeMethods.DISPLAY_DEVICE { cb = Marshal.SizeOf<NativeMethods.DISPLAY_DEVICE>() };

        for (uint i = 0; NativeMethods.EnumDisplayDevices(null, i, ref device, 0); i++)
        {
            result.Add(device);
            device.cb = Marshal.SizeOf<NativeMethods.DISPLAY_DEVICE>();
        }

        return result;
    }

    private static int? RunPnpUtil(string arguments)
    {
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "pnputil.exe",
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true
        });
        process?.WaitForExit();
        return process?.ExitCode;
    }
}
