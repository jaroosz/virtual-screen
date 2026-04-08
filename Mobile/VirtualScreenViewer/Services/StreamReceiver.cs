using System.Net.Sockets;
using VirtualScreenViewer.Core.Protocol;

namespace VirtualScreenViewer.Services;

public class StreamReceiver : IDisposable
{
    private UdpClient? _udpClient;
    private CancellationTokenSource? _cts;
    private Task? _receiveTask;
    private bool _connectionResponseReceived;

    private FrameAssembler? _currentAssembler;
    private uint _currentSeq = uint.MaxValue;

    public event EventHandler<FrameReadyEventArgs>? FrameReady;
    public event EventHandler<string>? LogMessage;
    public event EventHandler<ConnectionStatusEventArgs>? ConnectionStatusChanged;

    public bool IsConnected { get; private set; }

    public async Task ConnectAsync(string ip, int port)
    {
        _udpClient = new UdpClient();
        _udpClient.Client.ReceiveBufferSize = 4 * 1024 * 1024;
        _udpClient.Connect(ip, port);

        _cts = new CancellationTokenSource();
        _receiveTask = Task.Factory.StartNew(
            () => ReceiveLoop(_cts.Token),
            _cts.Token,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default).Unwrap();

        var req = new StreamPacket
        {
            Type = PacketType.ConnectionRequest,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        var reqBytes = req.ToBytes();
        await _udpClient.SendAsync(reqBytes, reqBytes.Length);

        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < deadline)
        {
            await Task.Delay(50);
            if (_connectionResponseReceived) { IsConnected = true; return; }
        }

        Disconnect();
        throw new Exception($"No response from {ip}:{port}");
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

                switch (packet.Type)
                {
                    case PacketType.ConnectionResponse:
                        _connectionResponseReceived = true;
                        break;

                    case PacketType.VideoFrame:
                        OnFrameComplete(packet.Payload, packet.FrameNumber, packet.Width, packet.Height, 1);
                        break;

                    case PacketType.VideoFrameFragment:
                        HandleFragment(packet);
                        break;
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { Log($"Error: {ex.Message}"); }
        }
    }

    private void HandleFragment(StreamPacket packet)
    {
        if (packet.SequenceNumber != _currentSeq)
        {
            _currentSeq = packet.SequenceNumber;
            _currentAssembler = new FrameAssembler(packet.TotalFragments, packet.FrameNumber, packet.Width, packet.Height, packet.Timestamp);
        }

        if (_currentAssembler == null) return;

        if (_currentAssembler.AddFragment(packet.FragmentIndex, packet.Payload))
        {
            var receiveTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var latency = receiveTime - _currentAssembler.SendTimestamp;
            Log($"✓ Frame #{_currentAssembler.FrameNumber} COMPLETE - latency: {latency}ms ({_currentAssembler.TotalFragments} fragments)");

            var data = _currentAssembler.GetData();
            OnFrameComplete(data, _currentAssembler.FrameNumber, _currentAssembler.Width, _currentAssembler.Height, _currentAssembler.TotalFragments);
            _currentAssembler = null;
            _currentSeq = uint.MaxValue;
        }
    }

    private void OnFrameComplete(byte[] data, uint frameNumber, int width, int height, int fragments)
    {
        // Log($"Frame #{frameNumber} completed ({fragments} frags, {data.Length} B)");
        FrameReady?.Invoke(this, new FrameReadyEventArgs
        {
            Data = data,
            FrameNumber = frameNumber,
            Width = width,
            Height = height
        });
    }

    private void Log(string msg) => LogMessage?.Invoke(this, msg);

    public void Disconnect()
    {
        _cts?.Cancel();
        _receiveTask?.Wait(2000);
        _udpClient?.Close();
        _udpClient = null;
        _cts = null;
        _currentAssembler = null;
        _currentSeq = uint.MaxValue;
        IsConnected = false;
        _connectionResponseReceived = false;
    }

    public void Dispose() => Disconnect();

    private class FrameAssembler
    {
        private readonly byte[][] _fragments;
        private readonly bool[] _received;
        private int _receivedCount;
        private int _totalBytes;

        public uint FrameNumber { get; }
        public int TotalFragments { get; }
        public int Width { get; }
        public int Height { get; }
        public long SendTimestamp { get; }

        public FrameAssembler(int totalFragments, uint frameNumber, int width, int height, long sendTimestamp)
        {
            _fragments = new byte[totalFragments][];
            _received = new bool[totalFragments];
            TotalFragments = totalFragments;
            FrameNumber = frameNumber;
            Width = width;
            Height = height;
            SendTimestamp = sendTimestamp;
        }

        public bool AddFragment(int index, byte[] data)
        {
            if (index >= _fragments.Length || _received[index]) return false;
            _fragments[index] = data;
            _received[index] = true;
            _receivedCount++;
            _totalBytes += data.Length;
            return _receivedCount == TotalFragments;
        }

        public byte[] GetData()
        {
            var result = new byte[_totalBytes];
            var offset = 0;
            foreach (var f in _fragments) { Buffer.BlockCopy(f, 0, result, offset, f.Length); offset += f.Length; }
            return result;
        }
    }
}

public class FrameReadyEventArgs : EventArgs
{
    public byte[] Data { get; init; } = Array.Empty<byte>();
    public uint FrameNumber { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
}

public enum ConnectionStatus { Disconnected, Connecting, Connected, Warning, Error }

public class ConnectionStatusEventArgs : EventArgs
{
    public ConnectionStatus Status { get; init; }
    public string Message { get; init; } = string.Empty;
}

public class FrameReceivedEventArgs : EventArgs
{
    public byte[] ImageData { get; init; } = Array.Empty<byte>();
    public int Width { get; init; }
    public int Height { get; init; }
    public uint SequenceNumber { get; init; }
    public long Timestamp { get; init; }
}