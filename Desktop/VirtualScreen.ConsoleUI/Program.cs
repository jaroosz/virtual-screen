using System.Runtime.InteropServices;
using VirtualScreen.App;
using VirtualScreen.Capture;
using VirtualScreen.Driver;
using VirtualScreen.Streaming;
using VirtualScreen.ConsoleUI;

[DllImport("user32.dll")]
static extern bool SetProcessDpiAwarenessContext(int value);

SetProcessDpiAwarenessContext(-4);

var port = 5555;
var portArg = args.FirstOrDefault(a => a.StartsWith("--port="));
if (portArg != null && int.TryParse(portArg["--port=".Length..], out var parsed))
    port = parsed;

var driverPath = Path.Combine(AppContext.BaseDirectory, "drivers");

var controller = new AppController(
    new DriverManager(driverPath),
    new WGCScreenCapture(),
    // new DxgiScreenCapture(),
    new UdpStreamServer());

var menu = new ConsoleMenu(controller, port);
await menu.RunAsync();