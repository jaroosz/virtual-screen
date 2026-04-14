using System;

namespace VirtualScreen.Core.Interface;

public interface IDriverManager
{
    // installation
    bool IsDriverInstalled();
    bool InstallDriver();
    bool UninstallDriver();

    // enable/disable driver
    bool IsDriverEnabled();
    bool EnableDriver();
    bool DisableDriver();

    // monitor
    string? GetVirtualMonitorDeviceName();
}
