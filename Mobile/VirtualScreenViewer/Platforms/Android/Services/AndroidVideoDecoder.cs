using Android.Media;
using VirtualScreenViewer.Services;

namespace VirtualScreenViewer.Platforms.Android.Services;

public class AndroidVideoDecoder : IVideoDecoder
{
    private MediaCodec? _decoder;
    private int _width;
    private int _height;
    private bool _isInitialized;

    // Rzeczywisty format zwrócony przez dekoder po OnOutputFormatChanged
    private int _actualColorFormat = 21;
    private int _actualStride = 0;
    private int _actualSliceHeight = 0;

    private int _inputCount = 0;
    private int _outputCount = 0;

    public event EventHandler<DecodedFrameEventArgs>? FrameDecoded;

    // Zdarzenie diagnostyczne — pojawia się w UI aplikacji
    public event EventHandler<string>? DiagnosticLog;
    private void Log(string msg)
    {
        global::Android.Util.Log.Debug("VSV", msg);
        DiagnosticLog?.Invoke(this, msg);
    }

    // BlockingCollection zamiast ConcurrentQueue — callback czeka na dane zamiast odrzucać
    private readonly System.Collections.Concurrent.BlockingCollection<(byte[] Data, uint FrameNumber)> _pendingInput
        = new(boundedCapacity: 8);

    public void Initialize(int width, int height)
    {
        _width = width;
        _height = height;

        _decoder = MediaCodec.CreateDecoderByType("video/hevc");

        var format = MediaFormat.CreateVideoFormat("video/hevc", width, height);

        // 21 = COLOR_FormatYUV420SemiPlanar (NV12)
        // Dekoder może wybrać inny — sprawdzimy w OnOutputFormatChanged
        format.SetInteger(MediaFormat.KeyColorFormat, 21);
        format.SetInteger(MediaFormat.KeyPriority, 0);

        if (OperatingSystem.IsAndroidVersionAtLeast(30))
            format.SetInteger(MediaFormat.KeyLowLatency, 1);

        _decoder.SetCallback(new DecoderCallback(this));
        _decoder.Configure(format, null, null, 0);
        _decoder.Start();

        _isInitialized = true;
        Log($"[Decoder] Started {width}x{height}");
    }

    public byte[]? DecodeFrame(byte[] h265Data, uint frameNumber = 0)
    {
        if (!_isInitialized) return null;

        var count = System.Threading.Interlocked.Increment(ref _inputCount);
        if (count <= 5)
        {
            Log($"[Decoder] Frame #{frameNumber}: {h265Data.Length} bytes, {GetNalInfo(h265Data)}");
        }

        if (!_pendingInput.TryAdd((h265Data, frameNumber), 50))
        {
            Log($"[Decoder] Queue full, dropping frame #{frameNumber}");
        }

        return null;
    }

    private static string GetNalInfo(byte[] data)
    {
        if (data.Length < 4) return "too short";

        // Szukaj start code 00 00 00 01 lub 00 00 01
        int start = -1;
        for (int i = 0; i < Math.Min(data.Length - 3, 16); i++)
        {
            if (data[i] == 0 && data[i + 1] == 0 && data[i + 2] == 0 && data[i + 3] == 1)
            { start = i + 4; break; }
            if (data[i] == 0 && data[i + 1] == 0 && data[i + 2] == 1)
            { start = i + 3; break; }
        }

        if (start < 0 || start >= data.Length)
            return $"no start code [{data[0]:X2} {data[1]:X2} {data[2]:X2} {data[3]:X2}]";

        int nalType = (data[start] >> 1) & 0x3F;
        string name = nalType switch
        {
            32 => "VPS",
            33 => "SPS",
            34 => "PPS",
            19 => "IDR_W_RADL",
            20 => "IDR_N_LP",
            1 => "TRAIL_R",
            0 => "TRAIL_N",
            _ => $"nal_{nalType}"
        };
        return $"{name} (byte=0x{data[start]:X2})";
    }

    private class DecoderCallback : MediaCodec.Callback
    {
        private readonly AndroidVideoDecoder _p;
        public DecoderCallback(AndroidVideoDecoder parent) => _p = parent;

        public override void OnInputBufferAvailable(MediaCodec codec, int index)
        {
            // Czekaj max 500ms na dane wejściowe
            if (!_p._pendingInput.TryTake(out var item, 500))
                return;

            try
            {
                var buf = codec.GetInputBuffer(index);
                if (buf == null) return;

                buf.Clear();
                var len = Math.Min(item.Data.Length, buf.Capacity());
                buf.Put(item.Data, 0, len);
                // Przekaż frameNumber jako presentationTimeUs (górne 32 bity)
                // żeby odzyskać go w OnOutputBufferAvailable
                long pts = ((long)item.FrameNumber << 32) | (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() & 0xFFFFFFFF);
                codec.QueueInputBuffer(index, 0, len,
                    pts, 0);
            }
            catch (Exception ex)
            {
                _p.Log($"[Decoder] InputBuffer error: {ex.Message}");
            }
        }

        public override void OnOutputBufferAvailable(MediaCodec codec, int index, MediaCodec.BufferInfo info)
        {
            var count = System.Threading.Interlocked.Increment(ref _p._outputCount);

            try
            {
                if (info.Size <= 0) { codec.ReleaseOutputBuffer(index, false); return; }

                var buf = codec.GetOutputBuffer(index);
                if (buf == null) { codec.ReleaseOutputBuffer(index, false); return; }

                uint frameNumber = (uint)(info.PresentationTimeUs >> 32);
                if (count <= 3 || count % 60 == 0)
                    _p.Log(
                        $"[Decoder] Output frame #{frameNumber}: size={info.Size} " +
                        $"fmt=0x{_p._actualColorFormat:X} stride={_p._actualStride} sliceH={_p._actualSliceHeight}");

                var yuv = new byte[info.Size];
                buf.Position(info.Offset);
                buf.Get(yuv, 0, info.Size);
                codec.ReleaseOutputBuffer(index, false);

                var rgba = _p.ConvertYuvToRgba(yuv, _p._width, _p._height,
                    _p._actualColorFormat, _p._actualStride, _p._actualSliceHeight);

                if (rgba != null)
                    _p.FrameDecoded?.Invoke(_p, new DecodedFrameEventArgs
                    {
                        RgbaData = rgba,
                        Width = _p._width,
                        Height = _p._height,
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        FrameNumber = (int)frameNumber
                    });
            }
            catch (Exception ex)
            {
                _p.Log($"[Decoder] OutputBuffer error: {ex.Message}");
                try { codec.ReleaseOutputBuffer(index, false); } catch { }
            }
        }

        public override void OnOutputFormatChanged(MediaCodec codec, MediaFormat fmt)
        {
            _p._actualColorFormat = fmt.ContainsKey(MediaFormat.KeyColorFormat) ? fmt.GetInteger(MediaFormat.KeyColorFormat) : 21;
            _p._actualStride = fmt.ContainsKey(MediaFormat.KeyStride) ? fmt.GetInteger(MediaFormat.KeyStride) : _p._width;
            _p._actualSliceHeight = fmt.ContainsKey(MediaFormat.KeySliceHeight) ? fmt.GetInteger(MediaFormat.KeySliceHeight) : _p._height;

            if (fmt.ContainsKey(MediaFormat.KeyWidth)) _p._width = fmt.GetInteger(MediaFormat.KeyWidth);
            if (fmt.ContainsKey(MediaFormat.KeyHeight)) _p._height = fmt.GetInteger(MediaFormat.KeyHeight);

            _p.Log(
                $"[Decoder] FormatChanged: {_p._width}x{_p._height} " +
                $"colorFmt=0x{_p._actualColorFormat:X}({_p._actualColorFormat}) " +
                $"stride={_p._actualStride} sliceH={_p._actualSliceHeight}");
        }

        public override void OnError(MediaCodec codec, MediaCodec.CodecException e)
        {
            _p.Log(
                $"[Decoder] CODEC ERROR: {e.Message} recoverable={e.IsRecoverable} transient={e.IsTransient}");
        }
    }

    private unsafe byte[]? ConvertYuvToRgba(byte[] yuv, int width, int height, int colorFmt, int stride, int sliceH)
    {
        int rowStride = stride > 0 ? stride : width;
        int sliceHeight = sliceH > 0 ? sliceH : height;

        // Zabezpieczenie — sprawdź czy bufor jest wystarczający
        int ySize = rowStride * sliceHeight;
        int uvSize = rowStride * (sliceHeight / 2);
        int minExpected = ySize + uvSize;
        if (yuv.Length < minExpected)
        {
            Log(
                $"[Decoder] YUV buffer too small: {yuv.Length} < {minExpected} " +
                $"(stride={rowStride} sliceH={sliceHeight})");
            // Spróbuj z width/height zamiast stride/sliceHeight
            rowStride = width;
            sliceHeight = height;
            ySize = width * height;
        }

        bool isSemiPlanar = colorFmt != 19; // 19 = I420 planar, reszta = SemiPlanar

        try
        {
            var rgba = new byte[width * height * 4];

            fixed (byte* src = yuv)
            fixed (byte* dst = rgba)
            {
                for (int row = 0; row < height; row++)
                {
                    for (int col = 0; col < width; col++)
                    {
                        int Y = src[row * rowStride + col];
                        int U, V;

                        if (isSemiPlanar)
                        {
                            // NV12: Y plane, potem przeplatane U,V,U,V...
                            byte* uvBase = src + ySize;
                            int uvOffset = (row / 2) * rowStride + (col & ~1);
                            U = uvBase[uvOffset] - 128;
                            V = uvBase[uvOffset + 1] - 128;
                        }
                        else
                        {
                            // I420: Y plane, potem cały U plane, potem cały V plane
                            int uvStride = (rowStride + 1) / 2;
                            int uvSH = (sliceHeight + 1) / 2;
                            byte* uBase = src + ySize;
                            byte* vBase = src + ySize + uvStride * uvSH;
                            int uvOff = (row / 2) * uvStride + col / 2;
                            U = uBase[uvOff] - 128;
                            V = vBase[uvOff] - 128;
                        }

                        int R = Y + ((359 * V) >> 8);
                        int G = Y - ((88 * U + 183 * V) >> 8);
                        int B = Y + ((454 * U) >> 8);

                        int idx = (row * width + col) * 4;
                        dst[idx] = (byte)(R < 0 ? 0 : R > 255 ? 255 : R);
                        dst[idx + 1] = (byte)(G < 0 ? 0 : G > 255 ? 255 : G);
                        dst[idx + 2] = (byte)(B < 0 ? 0 : B > 255 ? 255 : B);
                        dst[idx + 3] = 255;
                    }
                }
            }

            return rgba;
        }
        catch (Exception ex)
        {
            Log($"[Decoder] ConvertYUV error: {ex.Message}");
            return null;
        }
    }

    public void Dispose()
    {
        try
        {
            _isInitialized = false;
            _pendingInput.CompleteAdding();
            _decoder?.Stop();
            _decoder?.Release();
            _decoder?.Dispose();
            _decoder = null;
        }
        catch { }
    }
}