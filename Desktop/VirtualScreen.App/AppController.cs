using VirtualScreen.Core;

namespace VirtualScreen.App;

public class AppController
{
    private readonly IDriverManager _driverManager;
    private readonly IScreenCapture _screenCapture;
    private readonly IStreamServer _streamServer;

    public bool IsRunning { get; private set; }

    public AppController(
        IDriverManager driverManager,
        IScreenCapture screenCapture,
        IStreamServer streamServer)
    {
        _driverManager = driverManager;
        _screenCapture = screenCapture;
        _streamServer = streamServer;

        _screenCapture.FrameCaptured += OnFrameCaptured;
    }

    public async Task StartAsync(int port)
    {
        if (IsRunning) return;


        // check if virtual driver is installed. Install if not
        if (!_driverManager.IsDriverInstalled())
        {
            var installed = _driverManager.InstallDriver();
            if (!installed)
                throw new Exception("Cannot install driver.");

            await Task.Delay(2000);
        }

        // find virtual monitor
        var monitorName = _driverManager.GetVirtualMonitorDeviceName();
        if (monitorName == null)
            throw new Exception("Couldn't find virtual monitor.");

        // start HTTP server
        _streamServer.Start(port);

        // capture virtual monitor screen
        _screenCapture.Start(monitorName);

        IsRunning = true;
    }

    public void Stop()
    {
        if (!IsRunning) return;

        _screenCapture.Stop();
        _streamServer.Stop();

        IsRunning = false;
    }

    private void OnFrameCaptured(object? sender, FrameCapturedEventArgs e)
    {
        _streamServer.SendFrame(e.Data, e.Width, e.Height);
    }
}
