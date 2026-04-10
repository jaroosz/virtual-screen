using Android.Media;
using Android.Views;
using System.Collections.Concurrent;

namespace VirtualScreenViewer.Platforms.Android.Services;

public class AndroidVideoDecoder : IDisposable
{
    private MediaCodec? _decoder;
    private bool _initialized;

    private readonly ConcurrentQueue<(byte[] Data, uint FrameNumber, long AssemblyCompleteTime)> _pending = new();
    private readonly ConcurrentDictionary<long, (uint FrameNumber, long AssemblyCompleteTime, long QueuedTime)> _frameTimestamps = new();

    public event EventHandler<string>? DiagnosticLog;
    private void Log(string msg) => DiagnosticLog?.Invoke(this, msg);

    public void Initialize(Surface surface, int width, int height)
    {
        _decoder = MediaCodec.CreateDecoderByType("video/hevc");

        var format = MediaFormat.CreateVideoFormat("video/hevc", width, height);

        format.SetInteger(MediaFormat.KeyPriority, 0);

        if (OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            format.SetInteger(MediaFormat.KeyLowLatency, 1);
        }
        format.SetInteger(MediaFormat.KeyOperatingRate, int.MaxValue);
        format.SetInteger(MediaFormat.KeyMaxBFrames, 0);
        format.SetInteger(MediaFormat.KeyColorFormat, (int)MediaCodecCapabilities.Formatsurface);

        _decoder.SetCallback(new Callback(this));
        _decoder.Configure(format, surface, null, 0);
        _decoder.SetVideoScalingMode(VideoScalingMode.ScaleToFit);

        _decoder.Start();

        _initialized = true;
    }

    public void SubmitFrame(byte[] h265Data, uint frameNumber, long assemblyCompleteTime)
    {
        if (!_initialized) return;
        _pending.Enqueue((h265Data, frameNumber, assemblyCompleteTime));
    }

    private readonly ConcurrentQueue<(MediaCodec Codec, int Index)> _freeBuffers = new();

    private void FeedBuffer(MediaCodec codec, int index)
    {
        if (!_pending.TryDequeue(out var item))
        {
            _freeBuffers.Enqueue((codec, index));
            return;
        }

        try
        {
            var queueTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var buf = codec.GetInputBuffer(index);
            if (buf == null) return;

            buf.Clear();
            var len = Math.Min(item.Data.Length, buf.Capacity());
            buf.Put(item.Data, 0, len);

            long pts = queueTime * 1000;
            _frameTimestamps[pts] = (item.FrameNumber, item.AssemblyCompleteTime, queueTime);

            codec.QueueInputBuffer(index, 0, len, pts, 0);

            var queueingLatency = queueTime - item.AssemblyCompleteTime;
            if (queueingLatency > 5)
            {
                Log($"#{item.FrameNumber} waited {queueingLatency}ms in queue");
            }
        }
        catch (Exception ex) { Log($"Input error: {ex.Message}"); }
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
            var renderTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            if (info.Size > 0 && info.PresentationTimeUs > 0)
            {
                long pts = info.PresentationTimeUs;

                if (_p._frameTimestamps.TryRemove(pts, out var frameInfo))
                {
                    var totalLatency = renderTime - frameInfo.AssemblyCompleteTime;
                    var decodingLatency = renderTime - frameInfo.QueuedTime;

                    Log(_p, $"#{frameInfo.FrameNumber}: {totalLatency}ms (decode={decodingLatency}ms)");
                }
            }

            codec.ReleaseOutputBuffer(index, info.Size > 0);
        }

        public override void OnOutputFormatChanged(MediaCodec codec, MediaFormat format)
        {
            var w = format.ContainsKey(MediaFormat.KeyWidth) ? format.GetInteger(MediaFormat.KeyWidth) : -1;
            var h = format.ContainsKey(MediaFormat.KeyHeight) ? format.GetInteger(MediaFormat.KeyHeight) : -1;
            var color = format.ContainsKey(MediaFormat.KeyColorFormat) ? format.GetInteger(MediaFormat.KeyColorFormat) : -1;
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
            _frameTimestamps.Clear();
        }
        catch { }
    }
}