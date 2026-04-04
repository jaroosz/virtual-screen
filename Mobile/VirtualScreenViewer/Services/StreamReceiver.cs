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
    private DateTime _lastFrameReceived;
    private int _frameCount;
    private DateTime _fpsCounterStart;

    private readonly ConcurrentDictionary<uint, FrameAssembler> _frameAssemblers = new();

    public event EventHandler<FrameReceivedEventArgs>? FrameReceived;
    public event EventHandler<ConnectionStatusEventArgs>? ConnectionStatusChanged;

    public bool IsConnected { get; private set; }
    public int CurrentFps { get; private set; }
    public string? CurrentResolution { get; private set; }

    public async Task ConnectAsync(string desktopIp, int port)
    {
        if (IsConnected) return;

        try
        {
            // Status: Rozpoczęcie łączenia
            RaiseStatusChanged(ConnectionStatus.Connecting, $"Connecting to {desktopIp}:{port}...");

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

            RaiseStatusChanged(ConnectionStatus.Connecting, "Waiting for response...");

            // Czekaj na pierwszą ramkę przez max 5 sekund
            var waitTask = Task.Run(async () =>
            {
                for (int i = 0; i < 50; i++)
                {
                    await Task.Delay(100);
                    if (_lastFrameReceived != default)
                        return true;
                }
                return false;
            });

            var receivedData = await waitTask;

            if (receivedData)
            {
                IsConnected = true;
                _fpsCounterStart = DateTime.Now;
                RaiseStatusChanged(ConnectionStatus.Connected, $"Connected to {desktopIp}");
            }
            else
            {
                // Timeout - brak odpowiedzi
                Disconnect();
                RaiseStatusChanged(ConnectionStatus.Error, "Connection timeout - no response from server");
                throw new TimeoutException("No response from desktop. Make sure the desktop application is running.");
            }
        }
        catch (SocketException ex)
        {
            RaiseStatusChanged(ConnectionStatus.Error, $"Network error: {ex.Message}");
            Disconnect();
            throw new Exception($"Cannot connect to {desktopIp}:{port}. Check IP address and network.", ex);
        }
        catch (TimeoutException)
        {
            throw;
        }
        catch (Exception ex)
        {
            RaiseStatusChanged(ConnectionStatus.Error, $"Error: {ex.Message}");
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

                if (packet == null) continue;

                if (packet.Type == PacketType.VideoFrame)
                {
                    EmitFrame(packet);
                }
                else if (packet.Type == PacketType.VideoFrameFragment)
                {
                    HandleFragment(packet);
                }

                // Sprawdź timeout połączenia
                if (IsConnected && (DateTime.Now - _lastFrameReceived).TotalSeconds > 10)
                {
                    RaiseStatusChanged(ConnectionStatus.Warning, "No frames received for 10 seconds");
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                RaiseStatusChanged(ConnectionStatus.Error, $"Receive error: {ex.Message}");
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
        _lastFrameReceived = DateTime.Now;
        _frameCount++;

        // Aktualizuj rozdzielczość
        CurrentResolution = $"{packet.Width}x{packet.Height}";

        // Oblicz FPS co sekundę
        var elapsed = (DateTime.Now - _fpsCounterStart).TotalSeconds;
        if (elapsed >= 1.0)
        {
            CurrentFps = (int)(_frameCount / elapsed);
            _frameCount = 0;
            _fpsCounterStart = DateTime.Now;

            // Aktualizuj status z informacjami
            RaiseStatusChanged(ConnectionStatus.Connected,
                $"Connected • {CurrentResolution} • {CurrentFps} FPS");
        }

        FrameReceived?.Invoke(this, new FrameReceivedEventArgs
        {
            ImageData = packet.Payload,
            Width = packet.Width,
            Height = packet.Height,
            SequenceNumber = packet.SequenceNumber,
            Timestamp = packet.Timestamp
        });
    }

    private void RaiseStatusChanged(ConnectionStatus status, string message)
    {
        ConnectionStatusChanged?.Invoke(this, new ConnectionStatusEventArgs
        {
            Status = status,
            Message = message,
            Timestamp = DateTime.Now
        });
    }

    public void Disconnect()
    {
        if (!IsConnected && _udpClient == null) return;

        _cts?.Cancel();
        _receiveTask?.Wait(1000);
        _udpClient?.Close();

        _udpClient = null;
        _cts = null;
        _frameAssemblers.Clear();
        IsConnected = false;
        _lastFrameReceived = default;
        _frameCount = 0;
        CurrentFps = 0;
        CurrentResolution = null;

        RaiseStatusChanged(ConnectionStatus.Disconnected, "Disconnected");
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