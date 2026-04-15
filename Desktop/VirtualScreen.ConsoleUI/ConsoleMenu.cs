using VirtualScreen.App;
using VirtualScreen.Core;

namespace VirtualScreen.ConsoleUI;

public class ConsoleMenu
{
    private readonly AppController _controller;
    private readonly int _port;

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
                        await SelectMonitorAsync();
                    break;

                case '3':
                    if (!_controller.IsRunning)
                        await ToggleVirtualMonitorAsync();
                    break;

                case '4':
                    // Streaming settings - TODO
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

        WriteLine($"Streaming        [{(_controller.IsRunning ? "YES" : "NO")}]");
        WriteLine($"Virtual driver   [{(virtualEnabled ? "ENABLED" : "DISABLED")}]");
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
        WriteLine($"[2] Change monitor{(_controller.IsRunning ? " (stop streaming first)" : "")}");
        WriteLine($"[3] {(virtualEnabled ? "Disable" : "Enable")} virtual monitor{(_controller.IsRunning ? " (stop streaming first)" : "")}");
        WriteLine("[4] Streaming settings");
        WriteLine("[5] Exit");
        WriteLine("");
    }

    private async Task SelectMonitorAsync()
    {
        Console.Clear();
        WriteLine("Select monitor:");
        WriteLine("");

        var monitors = _controller.GetMonitors();
        for (var i = 0; i < monitors.Count; i++)
        {
            var tag = monitors[i].IsVirtual ? " (virtual)" : "";
            WriteLine($"[{i + 1}] {monitors[i].DeviceName}{tag}");
        }

        WriteLine("");
        Console.Write("Enter number: ");

        var input = Console.ReadLine();
        if (int.TryParse(input, out var index) && index >= 1 && index <= monitors.Count)
            _controller.SelectMonitor(monitors[index - 1].DeviceName);

        await Task.CompletedTask;
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
        Thread.Sleep(500);
    }

    private static void WriteLine(string text) =>
        Console.WriteLine(text.PadRight(Console.WindowWidth - 1));
}