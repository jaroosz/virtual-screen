using VirtualScreen.Core;
using VirtualScreen.Core.Interface;
using VirtualScreen.Streaming;

namespace VirtualScreen.App;

public class AppController
{
    private readonly IDriverManager _driverManager;
    private readonly IScreenCapture _screenCapture;
    private readonly IStreamServer _streamServer;

    private string? _selectedMonitor;

    public bool IsRunning { get; private set; }
    public string? SelectedMonitor => _selectedMonitor;

    public event EventHandler<AppEvent>? StatusChanged;

    public AppController(IDriverManager driverManager, IScreenCapture screenCapture, IStreamServer streamServer)
    {
        _driverManager = driverManager;
        _screenCapture = screenCapture;
        _streamServer = streamServer;
    }

    public async Task InitializeAsync()
    {
        if (_driverManager.IsDriverInstalled())
        {
            Emit(AppEventType.Info, "Driver already installed.");
            return;
        }

        Emit(AppEventType.Info, "Installing driver...");

        if (!_driverManager.InstallDriver())
        {
            Emit(AppEventType.Error, "Cannot install driver.");
            return;
        }

        await Task.Delay(2000);
        Emit(AppEventType.Success, "Driver installed successfully.");
    }

    public List<MonitorInfo> GetMonitors()
    {
        var virtualDeviceName = _driverManager.GetVirtualMonitorDeviceName();

        return MonitorHelper.GetMonitors().Select(m => new MonitorInfo(
            DeviceName: m.DeviceName,
            IsVirtual: !string.IsNullOrEmpty(virtualDeviceName) &&
                       string.Equals(m.DeviceName, virtualDeviceName, StringComparison.OrdinalIgnoreCase),
            IsStreaming: IsRunning &&
                         string.Equals(m.DeviceName, _selectedMonitor, StringComparison.OrdinalIgnoreCase),
            X: m.X,
            Y: m.Y,
            Width: m.Width,
            Height: m.Height
        )).ToList();
    }

    public void SelectMonitor(string deviceName)
    {
        _selectedMonitor = deviceName;
        Emit(AppEventType.Success, $"Selected monitor: {deviceName}");
    }

    public async Task AddVirtualMonitorAsync()
    {
        if (_driverManager.IsDriverInstalled() && _driverManager.IsDriverEnabled())
        {
            Emit(AppEventType.Warning, "Virtual monitor is already enabled.");
            return;
        }

        if (!_driverManager.IsDriverInstalled())
        {
            Emit(AppEventType.Info, "Installing driver...");

            if (!_driverManager.InstallDriver())
            {
                Emit(AppEventType.Error, "Cannot install driver.");
                return;
            }

            await Task.Delay(2000);
            Emit(AppEventType.Success, "Virtual monitor added.");
            return;
        }

        _driverManager.EnableDriver();
        Emit(AppEventType.Success, "Virtual monitor enabled.");
    }

    public void RemoveVirtualMonitor()
    {
        if (!_driverManager.IsDriverEnabled())
        {
            Emit(AppEventType.Warning, "Virtual monitor is already disabled.");
            return;
        }

        var virtualDeviceName = _driverManager.GetVirtualMonitorDeviceName();

        if (IsRunning &&
            string.Equals(_selectedMonitor, virtualDeviceName, StringComparison.OrdinalIgnoreCase))
        {
            Stop();
            Emit(AppEventType.Warning, "Streaming stopped because virtual monitor was removed.");
        }

        _driverManager.DisableDriver();
        Emit(AppEventType.Success, "Virtual monitor disabled.");
    }

    public void Start(int port)
    {
        if (IsRunning)
        {
            Emit(AppEventType.Warning, "Already streaming.");
            return;
        }

        if (_selectedMonitor == null)
        {
            Emit(AppEventType.Error, "No monitor selected.");
            return;
        }

        _streamServer.Start(port);

        var monitor = MonitorHelper.GetMonitors()
            .FirstOrDefault(m => m.DeviceName == _selectedMonitor);

        if (_streamServer is UdpStreamServer udpServer)
            udpServer.SetScreenCapture(_screenCapture, monitor?.X ?? 0, monitor?.Y ?? 0);

        _screenCapture.Start(_selectedMonitor);
        IsRunning = true;

        Emit(AppEventType.Success, $"Streaming started on port {port}.");
    }

    public void Stop()
    {
        if (!IsRunning)
        {
            Emit(AppEventType.Warning, "Not currently streaming.");
            return;
        }

        _screenCapture.Stop();
        _streamServer.Stop();
        IsRunning = false;

        Emit(AppEventType.Success, "Streaming stopped.");
    }

    private void Emit(AppEventType type, string message) =>
        StatusChanged?.Invoke(this, new AppEvent(type, message));
}

public record MonitorInfo(
    string DeviceName,
    bool IsVirtual,
    bool IsStreaming,
    int X, int Y,
    int Width, int Height
);