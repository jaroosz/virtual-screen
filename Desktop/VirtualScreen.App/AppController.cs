using VirtualScreen.Core;
using VirtualScreen.Streaming;
using VirtualScreen.Capture;

namespace VirtualScreen.App;

public class AppController
{
    private readonly IDriverManager _driverManager;
    private readonly IScreenCapture _screenCapture;
    private readonly IStreamServer _streamServer;

    private string? _selectedMonitor;
    public bool IsRunning { get; private set; }
    public string? SelectedMonitor => _selectedMonitor;

    public AppController(IDriverManager driverManager, IScreenCapture screenCapture, IStreamServer streamServer)
    {
        _driverManager = driverManager;
        _screenCapture = screenCapture;
        _streamServer = streamServer;
    }

    public async Task InitializeAsync()
    {
        if (_driverManager.IsDriverInstalled()) return;

        if (!_driverManager.InstallDriver())
            throw new Exception("Cannot install driver.");

        await Task.Delay(2000);
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

    public void SelectMonitor(string deviceName) =>
        _selectedMonitor = deviceName;

    public async Task AddVirtualMonitorAsync()
    {
        if (_driverManager.IsDriverInstalled() && _driverManager.IsDriverEnabled())
        {
            Console.WriteLine("Virtual monitor is enabled.");
            return;
        }

        if (!_driverManager.IsDriverInstalled())
        {
            if (!_driverManager.InstallDriver())
                throw new Exception("Cannot install driver.");

            await Task.Delay(2000);
            return;
        }

        _driverManager.EnableDriver();
    }

    public void RemoveVirtualMonitor()
    {
        if (!_driverManager.IsDriverEnabled())
            return;

        var virtualDeviceName = _driverManager.GetVirtualMonitorDeviceName();

        if (IsRunning &&
            string.Equals(_selectedMonitor, virtualDeviceName, StringComparison.OrdinalIgnoreCase))
        {
            Stop();
        }

        _driverManager.DisableDriver();
    }

    public async Task StartAsync(int port)
    {
        if (IsRunning) return;
        if (_selectedMonitor == null)
            throw new InvalidOperationException("No monitor selected.");

        _streamServer.Start(port);

        if (_streamServer is UdpStreamServer udpServer)
            udpServer.SetScreenCapture(_screenCapture);

        _screenCapture.Start(_selectedMonitor);
        IsRunning = true;

        await Task.CompletedTask;
    }

    public void Stop()
    {
        if (!IsRunning) return;

        _screenCapture.Stop();
        _streamServer.Stop();
        IsRunning = false;
    }
}

public record MonitorInfo(
                string DeviceName,      // monitor name
                bool IsVirtual,         // is it MttVDD?
                bool IsStreaming,       // is streaming?
                int X, int Y,           // position on windows monitor layout
                int Width, int Height   // resolution
    );
