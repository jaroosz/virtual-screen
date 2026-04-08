using Android.Media;
using Android.Views;

namespace VirtualScreenViewer.Platforms.Android.Services;

public class AndroidVideoDecoder : IDisposable
{
    private MediaCodec? _decoder;
    private bool _initialized;
    private int _inputCount;
    private int _outputCount;

    // Kolejka danych wejściowych — wolne buffery MediaCodec odbierają z niej dane
    private readonly System.Collections.Concurrent.ConcurrentQueue<(byte[] Data, uint FrameNumber)> _pending = new();

    public event EventHandler<string>? DiagnosticLog;
    private void Log(string msg) => DiagnosticLog?.Invoke(this, msg);

    public void Initialize(Surface surface, int width, int height)
    {
        _decoder = MediaCodec.CreateDecoderByType("video/hevc");

        var format = MediaFormat.CreateVideoFormat("video/hevc", width, height);
        format.SetInteger(MediaFormat.KeyPriority, 0);
        if (OperatingSystem.IsAndroidVersionAtLeast(30))
            format.SetInteger(MediaFormat.KeyLowLatency, 1);

        _decoder.SetCallback(new Callback(this));

        // Surface zamiast null — dekoder renderuje bezpośrednio na ekran, zero kopii
        _decoder.Configure(format, surface, null, 0);
        _decoder.Start();

        _initialized = true;
    }

    public void SubmitFrame(byte[] h265Data, uint frameNumber)
    {
        if (!_initialized) return;
        _pending.Enqueue((h265Data, frameNumber));
    }

    private readonly System.Collections.Concurrent.ConcurrentQueue<(MediaCodec Codec, int Index)> _freeBuffers = new();

    private void FeedBuffer(MediaCodec codec, int index)
    {
        if (!_pending.TryDequeue(out var item))
        {
            _freeBuffers.Enqueue((codec, index));
            return;
        }

        try
        {
            var buf = codec.GetInputBuffer(index);
            if (buf == null) return;
            buf.Clear();
            var len = Math.Min(item.Data.Length, buf.Capacity());
            buf.Put(item.Data, 0, len);
            long pts = ((long)item.FrameNumber << 32) | (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() & 0xFFFFFFFF);
            codec.QueueInputBuffer(index, 0, len, pts, 0);
        }
        catch (Exception ex) { Log($"[Decoder] Input error: {ex.Message}"); }
    }

    private class Callback : MediaCodec.Callback
    {
        private readonly AndroidVideoDecoder _p;
        public Callback(AndroidVideoDecoder p) => _p = p;

        public override void OnInputBufferAvailable(MediaCodec codec, int index)
        {
            _p.FeedBuffer(codec, index);
        }

        public override void OnOutputBufferAvailable(MediaCodec codec, int index, MediaCodec.BufferInfo info)
        {
            codec.ReleaseOutputBuffer(index, info.Size > 0);
        }

        public override void OnOutputFormatChanged(MediaCodec codec, MediaFormat format)
        {
            var w = format.ContainsKey(MediaFormat.KeyWidth) ? format.GetInteger(MediaFormat.KeyWidth) : -1;
            var h = format.ContainsKey(MediaFormat.KeyHeight) ? format.GetInteger(MediaFormat.KeyHeight) : -1;
            Log(_p, $"FormatChanged {w}x{h}");
        }

        public override void OnError(MediaCodec codec, MediaCodec.CodecException e)
        {
            Log(_p, $"ERROR: {e.Message}");
        }

        private static void Log(AndroidVideoDecoder p, string msg) => p.Log(msg);
    }

    public void TryFlushPending()
    {
        if (_freeBuffers.TryDequeue(out var free))
            FeedBuffer(free.Codec, free.Index);
    }

    public void Dispose()
    {
        try
        {
            _initialized = false;
            _decoder?.Stop();
            _decoder?.Release();
            _decoder?.Dispose();
            _decoder = null;
        }
        catch { }
    }
}