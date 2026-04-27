using VirtualScreen.App;
using VirtualScreen.Core;
using VirtualScreen.Encoding.Enums;

namespace VirtualScreen.ConsoleUI;

public class ConsoleMenu
{
    private readonly AppController _controller;
    private readonly int _port;
    private VideoCodec _currentCodec = VideoCodec.H265;

    public ConsoleMenu(AppController controller, int port)
    {
        _controller = controller;
        _port = port;

        _controller.StatusChanged += OnStatusChanged;
    }

    public async Task RunAsync()
    {
        await _controller.InitializeAsync();
        EnsureDefaultMonitorSelected();

        while (true)
        {
            Render();

            var key = Console.ReadKey(intercept: true).KeyChar;

            switch (key)
            {
                case '1':
                    if (_controller.IsRunning)
                        _controller.Stop();
                    else
                        _controller.Start(_port);
                    break;

                case '2':
                    if (!_controller.IsRunning)
                        SelectMonitor();
                    break;

                case '3':
                    if (!_controller.IsRunning)
                        await ToggleVirtualMonitorAsync();
                    break;

                case '4':
                    if (!_controller.IsRunning)
                        RenderSettings();
                    break;

                case '5':
                    _controller.Stop();
                    _controller.RemoveVirtualMonitor();
                    return;
            }
        }
    }

    private void Render()
    {
        Console.Clear();

        var monitors = _controller.GetMonitors();
        var virtualEnabled = monitors.Any(m => m.IsVirtual);

        WriteLine($"Streaming        [{(_controller.IsRunning ? "ON" : "OFF")}]");
        WriteLine($"Virtual monitor  [{(virtualEnabled ? "ENABLED" : "DISABLED")}]");
        WriteLine($"Codec            [{_currentCodec}]");
        WriteLine($"Streaming Client [NOT CONNECTED]");
        WriteLine("");
        WriteLine("Monitor list:");

        foreach (var m in monitors)
        {
            var selected = string.Equals(m.DeviceName, _controller.SelectedMonitor, StringComparison.OrdinalIgnoreCase);
            var tag = m.IsVirtual ? " (virtual)" : "";
            WriteLine($"[{(selected ? "X" : " ")}] {m.DeviceName}{tag}");
        }

        WriteLine("");
        WriteLine($"[1] {(_controller.IsRunning ? "Stop streaming" : "Start streaming")}");
        if (_controller.IsRunning)
        {
            WriteLineDisabled("[2] Change monitor (disabled)");
            WriteLineDisabled("[3] Toggle virtual monitor (disabled)");
            WriteLineDisabled("[4] Settings (disabled)");
        }
        else
        {
            WriteLine("[2] Change monitor");
            WriteLine("[3] Toggle virtual monitor");
            WriteLine("[4] Settings");
        }
        WriteLine("[5] Exit");
        WriteLine("");
    }

    private void SelectMonitor()
    {
        while (true)
        {
            Console.Clear();
            WriteLine("Select monitor");
            WriteLine("");

            var monitors = _controller.GetMonitors();
            for (var i = 0; i < monitors.Count; i++)
            {
                var selected = string.Equals(monitors[i].DeviceName,
                    _controller.SelectedMonitor, StringComparison.OrdinalIgnoreCase);
                var tag = monitors[i].IsVirtual ? " (virtual)" : "";
                WriteLine($"[{(selected ? "X" : " ")}] {monitors[i].DeviceName}{tag}");
            }

            WriteLine("");
            WriteLine("[0] Back");

            var key = Console.ReadKey(intercept: true).KeyChar;

            if (key == '0') return;

            if (int.TryParse(key.ToString(), out var index) && index >= 1 && index <= monitors.Count)
                _controller.SelectMonitor(monitors[index - 1].DeviceName);
        }
    }

    private async Task ToggleVirtualMonitorAsync()
    {
        var monitors = _controller.GetMonitors();
        var virtualEnabled = monitors.Any(m => m.IsVirtual);

        if (virtualEnabled)
            _controller.RemoveVirtualMonitor();
        else
            await _controller.AddVirtualMonitorAsync();
    }

    private void EnsureDefaultMonitorSelected()
    {
        if (_controller.SelectedMonitor != null) return;

        var primary = MonitorHelper.GetMonitors().FirstOrDefault(m => m.IsPrimary);
        if (primary != null)
            _controller.SelectMonitor(primary.DeviceName);
    }

    private void OnStatusChanged(object? sender, AppEvent e)
    {
        Console.ForegroundColor = e.Type switch
        {
            AppEventType.Success => ConsoleColor.Green,
            AppEventType.Warning => ConsoleColor.Yellow,
            AppEventType.Error => ConsoleColor.Red,
            _ => ConsoleColor.Gray
        };
        Console.WriteLine(e.Message);
        Console.ResetColor();
        Thread.Sleep(250);
    }

    private static void WriteLine(string text) =>
        Console.WriteLine(text.PadRight(Console.WindowWidth - 1));

    private static void WriteLineDisabled(string text)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(text.PadRight(Console.WindowWidth - 1));
        Console.ResetColor();
    }

    private void RenderSettings()
    {
        while (true)
        {
            Console.Clear();
            WriteLine("Settings");
            WriteLine("");
            WriteLine("[1] Codec");
            WriteLine("[2] not available");
            WriteLine("");
            WriteLine("[0] Back");

            var key = Console.ReadKey(intercept: true).KeyChar;
            switch (key)
            {
                case '1':
                    RenderCodecSettings();
                    break;
                case '0':
                    return;
            }
        }
    }

    private void RenderCodecSettings()
    {
        while (true)
        {
            Console.Clear();
            WriteLine("Codec");
            WriteLine("");

            var codecs = new[] { VideoCodec.H264, VideoCodec.H265 };
            for (var i = 0; i < codecs.Length; i++)
            {
                var selected = _currentCodec == codecs[i];
                WriteLine($"[{(selected ? "X" : " ")}] {codecs[i]}");
            }

            Console.ForegroundColor = ConsoleColor.DarkGray;
            WriteLine("[ ] AV1 (not available)");
            Console.ResetColor();

            WriteLine("");
            WriteLine("[0] Back");

            var key = Console.ReadKey(intercept: true).KeyChar;
            switch (key)
            {
                case '1':
                    _currentCodec = VideoCodec.H264;
                    _controller.SetCodec(_currentCodec);
                    break;
                case '2':
                    _currentCodec = VideoCodec.H265;
                    _controller.SetCodec(_currentCodec);
                    break;
                case '0':
                    return;
            }
        }
    }
}