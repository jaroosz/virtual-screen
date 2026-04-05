using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using System.Threading.Channels;
using VirtualScreenViewer.Core.Protocol;

namespace VirtualScreenViewer.Services;

public class StreamReceiver : IDisposable
{
    private UdpClient? _udpClient;
    private CancellationTokenSource? _cts;
    private Task? _receiveTask;
    private Task? _decodeTask;
    private bool _connectionResponseReceived;

    private Channel<StreamPacket>? _frameChannel;
    private readonly ConcurrentDictionary<uint, FrameAssembler> _frameAssemblers = new();

    private IVideoDecoder? _decoder;
    private bool _decoderInitialized = false;

    public event EventHandler<FrameReceivedEventArgs>? FrameReceived;
    public event EventHandler<DecodedFrameEventArgs>? DecodedFrameReceived;
    public event EventHandler<ConnectionStatusEventArgs>? ConnectionStatusChanged;

    public bool IsConnected { get; private set; }

    public StreamReceiver(IVideoDecoder? decoder = null)
    {
        _decoder = decoder;
    }

    public async Task ConnectAsync(string desktopIp, int port)
    {
        if (IsConnected) return;

        try
        {
            _udpClient = new UdpClient();
            _udpClient.Client.ReceiveBufferSize = 4 * 1024 * 1024; // 4MB
            _udpClient.Connect(desktopIp, port);

            _cts = new CancellationTokenSource();

            // channel with only 4 frames
            _frameChannel = Channel.CreateBounded<StreamPacket>(new BoundedChannelOptions(4)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = true
            });

            // for receiving UDP
            _receiveTask = Task.Factory.StartNew(
                () => ReceiveLoop(_cts.Token),
                _cts.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default).Unwrap();

            // for decoding
            _decodeTask = Task.Factory.StartNew(
                () => DecodeLoop(_cts.Token),
                _cts.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default).Unwrap();

            var requestPacket = new StreamPacket
            {
                Type = PacketType.ConnectionRequest,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            var data = requestPacket.ToBytes();
            await _udpClient.SendAsync(data, data.Length);

            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (DateTime.UtcNow < deadline)
            {
                await Task.Delay(50);
                if (_connectionResponseReceived)
                {
                    IsConnected = true;
                    return;
                }
            }

            Disconnect();
        }
        catch (SocketException ex)
        {
            Disconnect();
            throw new Exception($"Cannot connect to {desktopIp}:{port}.", ex);
        }
        catch
        {
            Disconnect();
            throw;
        }
    }

    private async Task ReceiveLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _udpClient != null)
        {
            try
            {
                var result = await _udpClient.ReceiveAsync(ct);
                var packet = StreamPacket.FromBytes(result.Buffer);

                if (packet == null)
                    continue;

                switch (packet.Type)
                {
                    case PacketType.ConnectionResponse:
                        _connectionResponseReceived = true;
                        break;

                    case PacketType.VideoFrame:
                        _frameChannel?.Writer.TryWrite(packet);
                        break;

                    case PacketType.VideoFrameFragment:
                        HandleFragment(packet);
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }

        _frameChannel?.Writer.TryComplete();
    }

    private void HandleFragment(StreamPacket packet)
    {
        var assembler = _frameAssemblers.GetOrAdd(
            packet.SequenceNumber,
            _ => new FrameAssembler(packet.TotalFragments, packet.Width, packet.Height, packet.Timestamp, packet.FrameNumber));

        if (assembler.AddFragment(packet.FragmentIndex, packet.Payload))
        {
            var completeFrame = new StreamPacket
            {
                Type = PacketType.VideoFrame,
                SequenceNumber = packet.SequenceNumber,
                Timestamp = assembler.Timestamp,
                Width = assembler.Width,
                Height = assembler.Height,
                FrameNumber = assembler.FrameNumber,
                Payload = assembler.GetCompleteData() // only once
            };

            _frameChannel?.Writer.TryWrite(completeFrame);
            _frameAssemblers.TryRemove(packet.SequenceNumber, out _);

            CleanupStaleAssemblers(packet.SequenceNumber);
        }
    }

    private void CleanupStaleAssemblers(uint currentSeq)
    {
        const uint staleThreshold = 30;
        foreach (var key in _frameAssemblers.Keys)
        {
            if (currentSeq - key > staleThreshold)
                _frameAssemblers.TryRemove(key, out _);
        }
    }

    private async Task DecodeLoop(CancellationToken ct)
    {
        if (_frameChannel == null)
            return;

        await foreach (var packet in _frameChannel.Reader.ReadAllAsync(ct))
        {
            try
            {
                if (_decoder != null && !_decoderInitialized && packet.Width > 0 && packet.Height > 0)
                {
                    try
                    {
                        _decoder.FrameDecoded += OnDecoderFrameDecoded;
                        _decoder.Initialize(packet.Width, packet.Height);
                        _decoderInitialized = true;
                        System.Diagnostics.Debug.WriteLine($"Decoder initialized");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Init error: {ex.Message}");
                    }
                }

                if (_decoder != null && _decoderInitialized)
                {
                    _decoder.DecodeFrame(packet.Payload, packet.FrameNumber);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }
    }

    private void OnDecoderFrameDecoded(object? sender, DecodedFrameEventArgs e)
    {
        DecodedFrameReceived?.Invoke(this, new DecodedFrameEventArgs
        {
            RgbaData = e.RgbaData,
            Width = e.Width,
            Height = e.Height,
            Timestamp = e.Timestamp,
            FrameNumber = e.FrameNumber
        });
    }

    public void Disconnect()
    {
        if (!IsConnected && _udpClient == null) return;

        _cts?.Cancel();
        _frameChannel?.Writer.TryComplete();

        _receiveTask?.Wait(2000);
        _decodeTask?.Wait(2000);

        _udpClient?.Close();
        _udpClient = null;
        _cts = null;
        _frameChannel = null;
        _frameAssemblers.Clear();

        IsConnected = false;
        _connectionResponseReceived = false;
        if (_decoder != null)
            _decoder.FrameDecoded -= OnDecoderFrameDecoded;

        _decoderInitialized = false;
    }

    public void Dispose()
    {
        Disconnect();
        _decoder?.Dispose();
    }

    private class FrameAssembler
    {
        private readonly byte[][] _fragments;
        private readonly bool[] _received;
        private int _receivedCount;
        private byte[]? _cachedResult;

        public int Width { get; }
        public int Height { get; }
        public long Timestamp { get; }
        public uint FrameNumber { get; }

        public FrameAssembler(int totalFragments, int width, int height, long timestamp, uint frameNumber = 0)
        {
            _fragments = new byte[totalFragments][];
            _received = new bool[totalFragments];
            Width = width;
            Height = height;
            Timestamp = timestamp;
            FrameNumber = frameNumber;
        }

        public bool AddFragment(int index, byte[] data)
        {
            if (index >= _fragments.Length || _received[index])
                return false;

            _fragments[index] = data;
            _received[index] = true;
            _receivedCount++;

            return _receivedCount == _fragments.Length;
        }

        public byte[] GetCompleteData()
        {
            if (_cachedResult != null) return _cachedResult;

            var totalSize = _fragments.Sum(f => f.Length);
            _cachedResult = new byte[totalSize];
            var offset = 0;

            foreach (var fragment in _fragments)
            {
                Array.Copy(fragment, 0, _cachedResult, offset, fragment.Length);
                offset += fragment.Length;
            }

            return _cachedResult;
        }
    }
}

public enum ConnectionStatus
{
    Disconnected,
    Connecting,
    Connected,
    Warning,
    Error
}

public class ConnectionStatusEventArgs : EventArgs
{
    public ConnectionStatus Status { get; init; }
    public string Message { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}

public class FrameReceivedEventArgs : EventArgs
{
    public byte[] ImageData { get; init; } = Array.Empty<byte>();
    public int Width { get; init; }
    public int Height { get; init; }
    public uint SequenceNumber { get; init; }
    public long Timestamp { get; init; }
}