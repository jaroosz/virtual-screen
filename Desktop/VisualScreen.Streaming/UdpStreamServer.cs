using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using VirtualScreen.Core;
using VirtualScreen.Core.Interface;
using VirtualScreen.Core.Protocol;
using VirtualScreen.Encoding;
using VirtualScreen.Encoding.Enums;

namespace VirtualScreen.Streaming;

public class UdpStreamServer : IStreamServer
{
    private UdpClient? _udpServer;
    private CancellationTokenSource? _cts;
    private Task? _listenerTask;
    private Task? _senderTask;
    private Task? _fpsReporterTask;
    private IPEndPoint? _clientEndpoint;
    private int _monitorX;
    private int _monitorY;

    private NvencEncoder? _encoder;
    private IScreenCapture? _screenCapture;
    private VideoCodec _codec = VideoCodec.H265;

    private Channel<byte[]>? _sendChannel;

    private uint _sequenceNumber;
    private int _lastWidth;
    private int _lastHeight;

    // FPS tracking
    private long _framesEnqueued;
    public int LastFps { get; private set; }

    private const int MaxUdpPayload = 1400;
    private const int Bitrate = 15_000_000;

    private static readonly long PacingIntervalTicks =
        (long)(Stopwatch.Frequency * (MaxUdpPayload * 8.0 / Bitrate) * 0.4);

    public bool IsRunning { get; private set; }
    public int Port { get; private set; }

    public void SetCodec(VideoCodec codec)
    {
        if (_codec == codec) return;

        _codec = codec;
        _encoder?.Dispose();
        _encoder = null;
    }

    public void Start(int port)
    {
        if (IsRunning) return;

        Port = port;
        _cts = new CancellationTokenSource();

        _udpServer = new UdpClient(port)
        {
            DontFragment = true
        };
        _udpServer.Client.SendBufferSize = 2 * 1024 * 1024;
        _udpServer.Client.ReceiveBufferSize = 256 * 1024;

        _sendChannel = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        _listenerTask = Task.Run(() => ListenForClients(_cts.Token));
        _senderTask = Task.Run(() => SendLoop(_cts.Token));
        _fpsReporterTask = Task.Run(() => FpsReporter(_cts.Token));

        IsRunning = true;
    }

    public void Stop()
    {
        if (!IsRunning) return;

        _cts?.Cancel();
        _sendChannel?.Writer.TryComplete();
        _udpServer?.Close();
        _encoder?.Dispose();

        _cts = null;
        _udpServer = null;
        _clientEndpoint = null;
        _sendChannel = null;
        _encoder = null;

        IsRunning = false;
    }

    public void SetScreenCapture(IScreenCapture screenCapture, int monitorX = 0, int monitorY = 0)
    {
        _screenCapture = screenCapture;
        _monitorX = monitorX;
        _monitorY = monitorY;
        _screenCapture.TextureCaptured += OnTextureCaptured;
    }

    private void OnTextureCaptured(object? sender, TextureCapturedEventArgs e)
    {
        if (_clientEndpoint == null || _sendChannel == null) return;

        // cursor-only
        if (e.TexturePtr == 0)
        {
            if (_lastWidth == 0 || _lastHeight == 0) return; // no frame size yet

            try
            {
                var (cx, cy, cursorType) = MonitorHelper.GetCursorInfo(_monitorX, _monitorY, _lastWidth, _lastHeight);
                var seq = Interlocked.Increment(ref _sequenceNumber) - 1;
                var packet = BuildPacket(
                    ReadOnlySpan<byte>.Empty,
                    (uint)seq,
                    DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    _lastWidth, _lastHeight,
                    0, 1, 0,
                    cx, cy, cursorType);
                if (_sendChannel.Writer.TryWrite(packet))
                    Interlocked.Increment(ref _framesEnqueued); // count cursor-only "frame"
            }
            catch { }

            return;
        }

        try
        {
            _lastWidth = e.Width;
            _lastHeight = e.Height;

            _encoder ??= new NvencEncoder(e.DevicePtr, e.Width, e.Height, _codec, Bitrate);

            var result = _encoder.EncodeTexture(e.TexturePtr);
            if (result == null) return;

            var (buffer, length, frameNumber) = result.Value;
            try
            {
                var (cx, cy, cursorType) = MonitorHelper.GetCursorInfo(_monitorX, _monitorY, e.Width, e.Height);
                EnqueueFrame(buffer, length, e.Width, e.Height, frameNumber, cx, cy, cursorType);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Encode/Send error: {ex.Message}");
        }
    }

    private void EnqueueFrame(
        byte[] h265Data, int length,
        int width, int height,
        uint frameNumber,
        short cursorX, short cursorY,
        CursorType cursorType)
    {
        // count one frame for FPS (before fragmenting)
        Interlocked.Increment(ref _framesEnqueued);

        var totalFragments = (ushort)Math.Ceiling((double)length / MaxUdpPayload);
        var seq = (uint)(Interlocked.Increment(ref _sequenceNumber) - 1);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        for (ushort i = 0; i < totalFragments; i++)
        {
            var offset = i * MaxUdpPayload;
            var payloadSize = Math.Min(MaxUdpPayload, length - offset);

            var packet = BuildPacket(
                h265Data.AsSpan(offset, payloadSize),
                seq, timestamp,
                width, height,
                i, totalFragments,
                frameNumber,
                cursorX, cursorY,
                cursorType);

            _sendChannel!.Writer.TryWrite(packet);
        }
    }

    private static byte[] BuildPacket(
        ReadOnlySpan<byte> payload,
        uint sequenceNumber, long timestamp,
        int width, int height,
        ushort fragmentIndex, ushort totalFragments,
        uint frameNumber,
        short cursorX, short cursorY,
        CursorType cursorType)
    {
        var packetSize = StreamPacket.HeaderSize + payload.Length;
        var rented = ArrayPool<byte>.Shared.Rent(packetSize);

        try
        {
            var span = rented.AsSpan(0, packetSize);
            var o = 0;

            span[o++] = (byte)PacketType.VideoFrame;
            BitConverter.TryWriteBytes(span.Slice(o, 4), sequenceNumber); o += 4;
            BitConverter.TryWriteBytes(span.Slice(o, 8), timestamp); o += 8;
            BitConverter.TryWriteBytes(span.Slice(o, 4), width); o += 4;
            BitConverter.TryWriteBytes(span.Slice(o, 4), height); o += 4;
            BitConverter.TryWriteBytes(span.Slice(o, 2), fragmentIndex); o += 2;
            BitConverter.TryWriteBytes(span.Slice(o, 2), totalFragments); o += 2;
            BitConverter.TryWriteBytes(span.Slice(o, 4), frameNumber); o += 4;
            BitConverter.TryWriteBytes(span.Slice(o, 2), cursorX); o += 2;
            BitConverter.TryWriteBytes(span.Slice(o, 2), cursorY); o += 2;
            span[o] = (byte)cursorType;

            payload.CopyTo(span.Slice(StreamPacket.HeaderSize));

            return span.ToArray();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    private void SendLoop(CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var nextSendTick = sw.ElapsedTicks;

        while (!ct.IsCancellationRequested)
        {
            if (!_sendChannel!.Reader.TryRead(out var packet))
            {
                Thread.SpinWait(10);
                continue;
            }

            while (sw.ElapsedTicks < nextSendTick)
                Thread.SpinWait(1);

            try { _udpServer?.Send(packet, packet.Length, _clientEndpoint); }
            catch { }

            nextSendTick += PacingIntervalTicks;
        }
    }

    private async Task FpsReporter(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(1000, ct);
                var frames = Interlocked.Exchange(ref _framesEnqueued, 0);
                LastFps = (int)Math.Min(int.MaxValue, frames);
                Console.WriteLine($"[FPS] Sent frames: {frames}/s");
            }
        }
        catch (OperationCanceledException) { }
    }

    private async Task ListenForClients(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _udpServer != null)
        {
            try
            {
                var result = await _udpServer.ReceiveAsync(ct);
                var packet = StreamPacket.FromBytes(result.Buffer);

                if (packet?.Type != PacketType.ConnectionRequest) continue;

                _clientEndpoint = result.RemoteEndPoint;
                _encoder?.ForceNextIDR();
                Console.WriteLine($"Client connected from {_clientEndpoint}");

                var response = new StreamPacket
                {
                    Type = PacketType.ConnectionResponse,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };
                var data = response.ToBytes();
                await _udpServer.SendAsync(data, data.Length, _clientEndpoint);
            }
            catch (OperationCanceledException) { break; }
            catch { }
        }
    }

    public void Dispose() => Stop();
}