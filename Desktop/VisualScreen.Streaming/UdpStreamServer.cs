using System.Buffers;
using System.Net;
using System.Net.Sockets;
using VirtualScreen.Core;
using VirtualScreen.Encoding;
using VirtualScreenViewer.Core.Protocol;

namespace VirtualScreen.Streaming;

public class UdpStreamServer : IStreamServer
{
    private UdpClient? _udpServer;
    private CancellationTokenSource? _cts;
    private Task? _listenerTask;
    private IPEndPoint? _clientEndpoint;
    private uint _sequenceNumber;

    private NvencH265Encoder? _encoder;
    private IScreenCapture? _screenCapture;

    private const int MaxUdpPayload = 1400; // MTU 1500 - 100 bytes
    private readonly byte[] _headerBuffer = new byte[100]; // pre-allocated header buffer

    public bool IsRunning { get; private set; }
    public int Port { get; private set; }

    public void Start(int port)
    {
        if (IsRunning) return;

        Port = port;
        _cts = new CancellationTokenSource();

        _udpServer = new UdpClient(port);

        _udpServer.Client.SendBufferSize = 2 * 1024 * 1024; // 2MB
        _udpServer.Client.ReceiveBufferSize = 256 * 1024;
        _udpServer.DontFragment = true;

        _listenerTask = Task.Run(() => ListenForClients(_cts.Token));

        IsRunning = true;
        Console.WriteLine($"UDP server listening on port {port}");
    }

    public void Stop()
    {
        if (!IsRunning) return;

        _cts?.Cancel();
        _udpServer?.Close();

        _encoder?.Dispose();
        _encoder = null;

        _cts = null;
        _udpServer = null;
        _clientEndpoint = null;

        IsRunning = false;
    }

    public void SetScreenCapture(IScreenCapture screenCapture)
    {
        _screenCapture = screenCapture;
        _screenCapture.TextureCaptured += OnTextureCaptured;
    }

    private void OnTextureCaptured(object? sender, TextureCapturedEventArgs e)
    {
        if (_clientEndpoint == null || _udpServer == null)
        {
            return;
        }

        try
        {
            if (_encoder == null)
            {
                _encoder = new NvencH265Encoder(e.DevicePtr, e.Width, e.Height, bitrate: 15_000_000);
            }

            var result = _encoder.EncodeTexture(e.TexturePtr);
            if (result == null) return;

            var (buffer, length, frameNumber) = result.Value;

            try
            {
                SendH265Frame(buffer, length, e.Width, e.Height, frameNumber);
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

    private void SendH265Frame(byte[] h265Data, int length, int width, int height, uint frameNumber = 0)
    {
        var totalFragments = (ushort)Math.Ceiling((double)length / MaxUdpPayload);
        var seqNum = _sequenceNumber++;
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        for (ushort i = 0; i < totalFragments; i++)
        {
            var offset = i * MaxUdpPayload;
            var payloadSize = Math.Min(MaxUdpPayload, length - offset);

            var packet = CreatePacketSpan(
                h265Data.AsSpan(offset, payloadSize),
                seqNum,
                timestamp,
                width,
                height,
                i,
                totalFragments,
                frameNumber
            );

            _udpServer!.Send(packet, packet.Length, _clientEndpoint!);
        }
    }

    private byte[] CreatePacketSpan(
    ReadOnlySpan<byte> payload,
    uint sequenceNumber,
    long timestamp,
    int width,
    int height,
    ushort fragmentIndex,
    ushort totalFragments,
    uint frameNumber = 0)
    {
        var packetSize = 50 + payload.Length;
        var packet = ArrayPool<byte>.Shared.Rent(packetSize);

        try
        {
            var span = packet.AsSpan(0, packetSize);
            var offset = 0;

            span[offset++] = (byte)(totalFragments > 1 ? 2 : 1); // VideoFrameFragment : VideoFrame

            // Sequence number (4 bytes)
            BitConverter.TryWriteBytes(span.Slice(offset, 4), sequenceNumber);
            offset += 4;

            // Timestamp (8 bytes)
            BitConverter.TryWriteBytes(span.Slice(offset, 8), timestamp);
            offset += 8;

            // Width (4 bytes)
            BitConverter.TryWriteBytes(span.Slice(offset, 4), width);
            offset += 4;

            // Height (4 bytes)
            BitConverter.TryWriteBytes(span.Slice(offset, 4), height);
            offset += 4;

            // Fragment index (2 bytes)
            BitConverter.TryWriteBytes(span.Slice(offset, 2), fragmentIndex);
            offset += 2;

            // Total fragments (2 bytes)
            BitConverter.TryWriteBytes(span.Slice(offset, 2), totalFragments);
            offset += 2;

            // FrameNumber (4 bytes)
            BitConverter.TryWriteBytes(span.Slice(offset, 4), frameNumber);
            offset += 4;

            // Padding to align to 50 bytes header
            offset = 50;

            // Payload (direct copy from H.265 buffer)
            payload.CopyTo(span.Slice(offset));

            // Return actual packet (not rented buffer)
            var finalPacket = span.ToArray();
            return finalPacket;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(packet);
        }
    }

    private async Task ListenForClients(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _udpServer != null)
        {
            try
            {
                var result = await _udpServer.ReceiveAsync(ct);
                var packet = StreamPacket.FromBytes(result.Buffer);

                if (packet?.Type == PacketType.ConnectionRequest)
                {
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
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch { }
        }
    }

    public void Dispose() => Stop();
}
