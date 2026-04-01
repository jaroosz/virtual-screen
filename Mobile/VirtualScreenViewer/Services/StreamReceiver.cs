using System.Net;
using System.Net.Sockets;
using VirtualScreenViewer.Core.Protocol;

namespace VirtualScreenViewer.Services;

public class StreamReceiver : IDisposable
{
    private UdpClient? _udpClient;
    private CancellationTokenSource? _cts;
    private Task? _receiveTask;

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

            // send connection request
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
        var endpoint = new IPEndPoint(IPAddress.Any, 0);

        while (!ct.IsCancellationRequested && _udpClient != null)
        {
            try
            {
                var result = await _udpClient.ReceiveAsync(ct);
                var packet = StreamPacket.FromBytes(result.Buffer);

                if (packet == null) continue;

                if (packet.Type == PacketType.VideoFrame)
                {
                    FrameReceived?.Invoke(this, new FrameReceivedEventArgs
                    {
                        ImageData = packet.PayLoad,
                        Width = packet.Width,
                        Height = packet.Height,
                        SequenceNumber = packet.SequenceNumber,
                        Timestamp = packet.Timestamp
                    });
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

    public void Disconnect()
    {
        if (!IsConnected) return;

        _cts?.Cancel();
        _receiveTask?.Wait(1000);
        _udpClient?.Close();

        _udpClient = null;
        _cts = null;
        IsConnected = false;

        ConnectionStatusChanged?.Invoke(this, "Disconnected");
    }

    public void Dispose()
    {
        Disconnect();
    }

    public class FrameReceivedEventArgs : EventArgs
    {
        public byte[] ImageData { get; init; } = Array.Empty<byte>();
        public int Width { get; init; }
        public int Height { get; init; }
        public uint SequenceNumber { get; init; }
        public long Timestamp { get; init; }
    }
}
