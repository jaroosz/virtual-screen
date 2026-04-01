using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using VirtualScreenViewer.Core.Protocol;

namespace VirtualScreenViewer.Services;

public class StreamReceiver : IDisposable
{
    private UdpClient? _udpClient;
    private CancellationTokenSource? _cts;
    private Task? _receiveTask;

    private readonly ConcurrentDictionary<uint, FrameAssembler> _frameAssemblers = new();

    public event EventHandler<FrameReceivedEventArgs>? FrameReceived;
    public event EventHandler<string>? ConnectionStatusChanged;

    public bool IsConnected { get; private set; }

    public async Task ConnectAsync(string desktopIp, int port)
    {
        if (IsConnected) return;

        try
        {
            _udpClient = new UdpClient();
            _udpClient.Client.ReceiveBufferSize = 1024 * 1024;
            _udpClient.Connect(desktopIp, port);

            _cts = new CancellationTokenSource();
            _receiveTask = Task.Run(() => ReceiveLoop(_cts.Token));

            var requestPacket = new StreamPacket
            {
                Type = PacketType.ConnectionRequest,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            var data = requestPacket.ToBytes();
            await _udpClient.SendAsync(data, data.Length);

            IsConnected = true;
            ConnectionStatusChanged?.Invoke(this, "Connected");
        }
        catch (Exception ex)
        {
            ConnectionStatusChanged?.Invoke(this, $"Error: {ex.Message}");
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

                if (packet == null) continue;

                if (packet.Type == PacketType.VideoFrame)
                {
                    EmitFrame(packet);
                }
                else if (packet.Type == PacketType.VideoFrameFragment)
                {
                    HandleFragment(packet);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                ConnectionStatusChanged?.Invoke(this, $"Receive error: {ex.Message}");
            }
        }
    }

    private void HandleFragment(StreamPacket packet)
    {
        var assembler = _frameAssemblers.GetOrAdd(
            packet.SequenceNumber,
            _ => new FrameAssembler(packet.TotalFragments, packet.Width, packet.Height, packet.Timestamp));

        if (assembler.AddFragment(packet.FragmentIndex, packet.Payload))
        {
            // Wszystkie fragmenty zebrane
            var completeFrame = new StreamPacket
            {
                Type = PacketType.VideoFrame,
                SequenceNumber = packet.SequenceNumber,
                Timestamp = assembler.Timestamp,
                Width = assembler.Width,
                Height = assembler.Height,
                Payload = assembler.GetCompleteData()
            };

            EmitFrame(completeFrame);
            _frameAssemblers.TryRemove(packet.SequenceNumber, out _);
        }
    }

    private void EmitFrame(StreamPacket packet)
    {
        FrameReceived?.Invoke(this, new FrameReceivedEventArgs
        {
            ImageData = packet.Payload,
            Width = packet.Width,
            Height = packet.Height,
            SequenceNumber = packet.SequenceNumber,
            Timestamp = packet.Timestamp
        });
    }

    public void Disconnect()
    {
        if (!IsConnected) return;

        _cts?.Cancel();
        _receiveTask?.Wait(1000);
        _udpClient?.Close();

        _udpClient = null;
        _cts = null;
        _frameAssemblers.Clear();
        IsConnected = false;

        ConnectionStatusChanged?.Invoke(this, "Disconnected");
    }

    public void Dispose()
    {
        Disconnect();
    }

    private class FrameAssembler
    {
        private readonly byte[][] _fragments;
        private readonly bool[] _received;
        private int _receivedCount;

        public int Width { get; }
        public int Height { get; }
        public long Timestamp { get; }

        public FrameAssembler(int totalFragments, int width, int height, long timestamp)
        {
            _fragments = new byte[totalFragments][];
            _received = new bool[totalFragments];
            Width = width;
            Height = height;
            Timestamp = timestamp;
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
            var totalSize = _fragments.Sum(f => f.Length);
            var result = new byte[totalSize];
            var offset = 0;

            foreach (var fragment in _fragments)
            {
                Array.Copy(fragment, 0, result, offset, fragment.Length);
                offset += fragment.Length;
            }

            return result;
        }
    }
}

public class FrameReceivedEventArgs : EventArgs
{
    public byte[] ImageData { get; init; } = Array.Empty<byte>();
    public int Width { get; init; }
    public int Height { get; init; }
    public uint SequenceNumber { get; init; }
    public long Timestamp { get; init; }
}