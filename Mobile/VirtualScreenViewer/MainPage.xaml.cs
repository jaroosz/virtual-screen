using VirtualScreenViewer.Services;
using Android.Widget;


#if ANDROID
using Android.Views;
using VirtualScreenViewer.Platforms.Android;
using VirtualScreenViewer.Platforms.Android.Services;
#endif

namespace VirtualScreenViewer;

public partial class MainPage : ContentPage
{
    private readonly StreamReceiver _receiver = new();
    private const int StreamPort = 5555;

    private readonly object _logLock = new();
    private readonly List<string> _logBuffer = new();
    private DateTime _lastFlush = DateTime.MinValue;

#if ANDROID
    private AndroidVideoDecoder? _decoder;
    private bool _decoderInitialized;
    private Surface? _surface;
#endif

    public MainPage()
    {
        InitializeComponent();

        _receiver.LogMessage += (_, msg) => AddLog(msg);
        _receiver.FrameReady += OnFrameReady;

#if ANDROID
        VideoView.HandlerChanged += OnVideoViewHandlerChanged;

        _decoder = new AndroidVideoDecoder();
        _decoder.DiagnosticLog += (_, msg) => AddLog(msg);
#endif
    }

#if ANDROID
    private void OnVideoViewHandlerChanged(object? sender, EventArgs e)
    {
        if (VideoView.Handler is not VideoSurfaceViewHandler handler) return;

        var surfaceView = handler.PlatformView;
        surfaceView.Holder!.AddCallback(new SurfaceCallback(this));
    }

    private void OnSurfaceReady(Surface surface)
    {
        _surface = surface;
        AddLog("Surface ready");
    }

    private void OnFrameReady(object? sender, FrameReadyEventArgs e)
    {
        if (_decoder == null || _surface == null) return;

        if (!_decoderInitialized)
        {
            _decoder.Initialize(_surface, e.Width, e.Height);
            _decoderInitialized = true;
            AddLog($"Decoder initialized {e.Width}x{e.Height}");
        }

        _decoder.SubmitFrame(e.Data, e.FrameNumber, e.AssemblyCompleteTimestamp);
        _decoder.TryFlushPending();
    }

    private class SurfaceCallback : Java.Lang.Object, ISurfaceHolderCallback
    {
        private readonly MainPage _page;
        public SurfaceCallback(MainPage page) => _page = page;

        public void SurfaceCreated(ISurfaceHolder holder)
            => _page.OnSurfaceReady(holder.Surface!);

        public void SurfaceChanged(ISurfaceHolder holder, global::Android.Graphics.Format format, int w, int h) { }

        public void SurfaceDestroyed(ISurfaceHolder holder)
            => _page._surface = null;
    }
#else
    private void OnFrameReady(object? sender, FrameReadyEventArgs e) { }
#endif

    private async void OnConnectClicked(object sender, EventArgs e)
    {
        if (_receiver.IsConnected)
        {
            _receiver.Disconnect();
            ConnectButton.Text = "Connect";
#if ANDROID
            _decoderInitialized = false;
#endif
            return;
        }

        var ip = IpAddressEntry.Text?.Trim();
        if (string.IsNullOrEmpty(ip)) { await DisplayAlert("Error", "Enter IP", "OK"); return; }

        try
        {
            ConnectButton.IsEnabled = false;
            AddLog($"Connecting to {ip}:{StreamPort}...");
            await _receiver.ConnectAsync(ip, StreamPort);
            ConnectButton.Text = "Disconnect";
            AddLog("Connected!");
        }
        catch (Exception ex) { await DisplayAlert("Error", ex.Message, "OK"); }
        finally { ConnectButton.IsEnabled = true; }
    }

    private void AddLog(string message)
    {
        var entry = $"[{DateTime.Now:HH:mm:ss}] {message}";
        bool flush;
        lock (_logLock)
        {
            _logBuffer.Add(entry);
            flush = (DateTime.Now - _lastFlush).TotalMilliseconds >= 300;
        }
        if (flush) FlushLogs();
    }

    private void FlushLogs()
    {
        string[] snap;
        lock (_logLock)
        {
            if (_logBuffer.Count == 0) return;
            _lastFlush = DateTime.Now;
            snap = _logBuffer.ToArray();
            _logBuffer.Clear();
        }
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var lines = (StatusLabel.Text ?? "").Split('\n').ToList();
            lines.AddRange(snap);
            if (lines.Count > 80) lines.RemoveRange(0, lines.Count - 80);
            StatusLabel.Text = string.Join("\n", lines);
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _receiver.Dispose();
#if ANDROID
        _decoder?.Dispose();
#endif
    }
}