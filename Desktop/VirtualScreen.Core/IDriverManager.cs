using System;

namespace VirtualScreen.Core;

public interface IDriverManager
{
    bool IsDriverInstalled();
    bool InstallDriver();
    bool UninstallDriver();
    string? GetVirtualMonitorDeviceName();
}
