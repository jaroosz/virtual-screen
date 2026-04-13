using System.Diagnostics;
using System.Runtime.InteropServices;
using VirtualScreen.Core;

namespace VirtualScreen.Driver;

public class DriverManager : IDriverManager
{
    private readonly string _driverPath;
    private const string DeviceInstanceId = "ROOT\\DISPLAY\\0000";
    private const string HardwareId = "Root\\MttVDD";

    public DriverManager(string driverPath)
    {
        _driverPath = driverPath;
    }

    // driver installation
    public bool IsDriverInstalled()
    {
        return GetAllDisplayDevices().Any(d =>
            d.DeviceID.Contains("MttVDD", StringComparison.OrdinalIgnoreCase));
    }

    public bool InstallDriver()
    {
        var infPath = Path.Combine(_driverPath, "MttVDD.inf");
        if (!File.Exists(infPath))
            return false;

        var addDriver = Process.Start(new ProcessStartInfo
        {
            FileName = "pnputil.exe",
            Arguments = $"/add-driver \"{infPath}\" /install",
            UseShellExecute = false,
            CreateNoWindow = true
        });
        addDriver?.WaitForExit();

        var scan = Process.Start(new ProcessStartInfo
        {
            FileName = "pnputil.exe",
            Arguments = "/scan-devices",
            UseShellExecute = false,
            CreateNoWindow = true
        });
        scan?.WaitForExit();

        return addDriver?.ExitCode is 0 or 259;
    }

    public bool UninstallDriver()
    {
        var result = Process.Start(new ProcessStartInfo
        {
            FileName = "pnputil.exe",
            Arguments = "/delete-driver MttVDD.inf /uninstall",
            UseShellExecute = false,
            CreateNoWindow = true
        });

        result?.WaitForExit();
        return result?.ExitCode == 0;
    }

    // enable/disable driver
    public bool IsDriverEnabled()
    {
        throw new NotImplementedException();
    }

    public bool EnableDriver()
    {
        throw new NotImplementedException();
    }

    public bool DisableDriver()
    {
        throw new NotImplementedException();
    }

    // monitor
    public string? GetVirtualMonitorDeviceName()
    {
        var device = GetAllDisplayDevices()
            .FirstOrDefault(d => d.DeviceID.Contains("MttVDD", StringComparison.OrdinalIgnoreCase));

        if (device.DeviceName == null)
            return null;

        return device.DeviceName;
    }

    private List<NativeMethods.DISPLAY_DEVICE> GetAllDisplayDevices()
    {
        var result = new List<NativeMethods.DISPLAY_DEVICE>();
        var device = new NativeMethods.DISPLAY_DEVICE();
        device.cb = Marshal.SizeOf(device);

        uint i = 0;
        while (NativeMethods.EnumDisplayDevices(null, i, ref device, 0))
        {
            result.Add(device);
            i++;
        }
        return result;
    }
}
