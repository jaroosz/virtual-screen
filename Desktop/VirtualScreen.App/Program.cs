using System.Runtime.InteropServices;
using VirtualScreen.App;
using VirtualScreen.Capture;
using VirtualScreen.Driver;
using VirtualScreen.Streaming;

[DllImport("user32.dll")]
static extern bool SetProcessDpiAwarenessContext(int value);

SetProcessDpiAwarenessContext(-4);

var port = 8888;
var portArg = args.FirstOrDefault(a => a.StartsWith("--port="));
if (portArg != null && int.TryParse(portArg["--port=".Length..], out var parsed))
    port = parsed;

var driverPath = Path.Combine(AppContext.BaseDirectory, "drivers");

var controller = new AppController(
    new DriverManager(driverPath),
    new DxgiScreenCapture(),
    new UdpStreamServer());
    //new MjpegStreamServer());

var thread = new Thread(() =>
{
    controller.StartAsync(port).GetAwaiter().GetResult();
});
thread.SetApartmentState(ApartmentState.STA);
thread.Start();
thread.Join();

Console.WriteLine($"  Local:   http://localhost:{port}/");
Console.WriteLine("Press Ctrl+C to stop.");

var tcs = new TaskCompletionSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    tcs.SetResult();
};

await tcs.Task;
controller.Stop();