using SkiaSharp;
using SkiaSharp.Views.Maui;
using System.Collections.ObjectModel;
using VirtualScreenViewer.Services;

namespace VirtualScreenViewer;

public partial class MainPage : ContentPage
{
    private readonly StreamReceiver _receiver;
    private const int StreamPort = 5555;

    private SKBitmap? _currentFrame;
    private readonly object _frameLock = new();

    // Log throttling — UI nie jest zalewane tysiącami wpisów
    private readonly object _logLock = new();
    private readonly List<string> _logBuffer = new();
    private DateTime _lastLogFlush = DateTime.MinValue;
    private const int MaxLogLines = 100;
    private const int LogFlushIntervalMs = 500; // flush co 500ms

    // Statystyki klatek
    private int _framesReceived = 0;
    private DateTime _fpsCountStart = DateTime.Now;

    public MainPage()
    {
        InitializeComponent();

#if ANDROID
        var decoder = new VirtualScreenViewer.Platforms.Android.Services.AndroidVideoDecoder();
        decoder.DiagnosticLog += (_, msg) => AddLog(msg);
        _receiver = new StreamReceiver(decoder);
#else
        _receiver = new StreamReceiver();
#endif

        _receiver.DecodedFrameReceived += OnDecodedFrameReceived;
        _receiver.ConnectionStatusChanged += OnConnectionStatusChanged;
    }

    private async void OnConnectClicked(object sender, EventArgs e)
    {
        if (_receiver.IsConnected)
        {
            _receiver.Disconnect();
            ConnectButton.Text = "Connect";

            lock (_frameLock)
            {
                _currentFrame?.Dispose();
                _currentFrame = null;
            }
            VideoCanvas.InvalidateSurface();
            return;
        }

        var ipAddress = IpAddressEntry.Text?.Trim();
        if (string.IsNullOrEmpty(ipAddress))
        {
            await DisplayAlert("Error", "Please enter desktop IP address", "OK");
            return;
        }

        try
        {
            ConnectButton.IsEnabled = false;
            AddLog($"Connecting to {ipAddress}:{StreamPort}...");
            await _receiver.ConnectAsync(ipAddress, StreamPort);
            ConnectButton.Text = "Disconnect";
            AddLog("Connected!");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Connection Error", ex.Message, "OK");
        }
        finally
        {
            ConnectButton.IsEnabled = true;
        }
    }

    // Wywołane z wątku dekodującego — NIE UI thread
    private void OnDecodedFrameReceived(object? sender, DecodedFrameEventArgs e)
    {
        var frameNum = e.FrameNumber;
        _framesReceived++;

        // Aktualizuj bitmapę
        lock (_frameLock)
        {
            _currentFrame?.Dispose();
            _currentFrame = new SKBitmap(e.Width, e.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
            var pixels = _currentFrame.GetPixels();
            System.Runtime.InteropServices.Marshal.Copy(e.RgbaData, 0, pixels, e.RgbaData.Length);
        }

        // Oblicz FPS co sekundę
        var now = DateTime.Now;
        var elapsed = (now - _fpsCountStart).TotalSeconds;
        string? fpsLog = null;
        if (elapsed >= 1.0)
        {
            var fps = _framesReceived / elapsed;
            fpsLog = $"Frame #{frameNum} | FPS: {fps:F1} | {e.Width}x{e.Height}";
            _framesReceived = 0;
            _fpsCountStart = now;
        }

        // Tylko co sekundę loguj info o klatce (nie przy każdej)
        if (fpsLog != null)
            AddLog(fpsLog);

        // Odświeżaj canvas na UI thread — InvalidateSurface jest tanie
        MainThread.BeginInvokeOnMainThread(() => VideoCanvas.InvalidateSurface());
    }

    // Gorąca ścieżka — zero logów, zero alokacji poza rysowaniem
    private void OnCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Black);

        lock (_frameLock)
        {
            if (_currentFrame == null) return;

            var destRect = GetDestinationRect(
                _currentFrame.Width,
                _currentFrame.Height,
                e.Info.Width,
                e.Info.Height);

            canvas.DrawBitmap(_currentFrame, destRect);
        }
    }

    private static SKRect GetDestinationRect(int imageWidth, int imageHeight, int canvasWidth, int canvasHeight)
    {
        var imageAspect = (float)imageWidth / imageHeight;
        var canvasAspect = (float)canvasWidth / canvasHeight;

        float width, height;
        if (imageAspect > canvasAspect)
        {
            width = canvasWidth;
            height = canvasWidth / imageAspect;
        }
        else
        {
            height = canvasHeight;
            width = canvasHeight * imageAspect;
        }

        var x = (canvasWidth - width) / 2;
        var y = (canvasHeight - height) / 2;
        return new SKRect(x, y, x + width, y + height);
    }

    private void OnConnectionStatusChanged(object? sender, ConnectionStatusEventArgs e)
    {
        var emoji = e.Status switch
        {
            ConnectionStatus.Disconnected => "[disconnected]",
            ConnectionStatus.Connecting => "[connecting]",
            ConnectionStatus.Connected => "[connected]",
            ConnectionStatus.Warning => "[warning]",
            ConnectionStatus.Error => "[error]",
            _ => "[?]"
        };
        AddLog($"{emoji} {e.Message}");
    }

    // Buforowane logowanie — UI thread jest aktualizowany max co 500ms
    private void AddLog(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var entry = $"[{timestamp}] {message}";

        bool shouldFlush;
        lock (_logLock)
        {
            _logBuffer.Add(entry);
            shouldFlush = (DateTime.Now - _lastLogFlush).TotalMilliseconds >= LogFlushIntervalMs;
        }

        if (shouldFlush)
            FlushLogs();
    }

    private void FlushLogs()
    {
        string[] snapshot;
        lock (_logLock)
        {
            if (_logBuffer.Count == 0) return;
            _lastLogFlush = DateTime.Now;
            snapshot = _logBuffer.ToArray();
            _logBuffer.Clear();
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            var current = StatusLabel.Text ?? "";
            var lines = current.Split('\n').ToList();
            lines.AddRange(snapshot);

            while (lines.Count > MaxLogLines)
                lines.RemoveAt(0);

            StatusLabel.Text = string.Join("\n", lines);
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        lock (_frameLock)
        {
            _currentFrame?.Dispose();
            _currentFrame = null;
        }

        _receiver.Dispose();
    }
}