using System.Net;
using System.Net.Sockets;
using VirtualScreen.Core;
using VirtualScreenViewer.Core.Protocol;

namespace VirtualScreen.Streaming;

public class UdpStreamServer : IStreamServer
{
    private UdpClient? _udpServer;
    private CancellationTokenSource? _cts;
    private Task? _listenerTask;
    private IPEndPoint? _clientEndpoint;
    private uint _sequenceNumber;

    public bool IsRunning { get; private set; }
    public int Port { get; private set; }

    public void Start(int port)
    {
        if (IsRunning) return;

        Port = port;
        _cts = new CancellationTokenSource();

        _udpServer = new UdpClient(port);
        _udpServer.Client.SendBufferSize = 1024 * 1024;

        _listenerTask = Task.Run(() => ListenForClients(_cts.Token));

        IsRunning = true;
        Console.WriteLine($"UDP server listening on port {port}");
    }

    public void Stop()
    {
        if (!IsRunning) return;

        _cts?.Cancel();
        _udpServer?.Close();

        _cts = null;
        _udpServer = null;
        _clientEndpoint = null;

        IsRunning = false;
    }

    public void SendFrame(byte[] frameData, int width, int height)
    {
        if (_clientEndpoint == null || _udpServer == null) return;

        try
        {
            // Convert raw BGRA to JPEG (keep existing logic from MjpegStreamServer)
            var jpegData = ConvertToJpeg(frameData, width, height);

            var packet = new StreamPacket
            {
                Type = PacketType.VideoFrame,
                SequenceNumber = _sequenceNumber++,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Width = width,
                Height = height,
                Payload = jpegData
            };

            var data = packet.ToBytes();
            _udpServer.Send(data, data.Length, _clientEndpoint);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Send error: {ex.Message}");
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
                    Console.WriteLine($"Client connected from {_clientEndpoint}");

                    // Send response
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

    private byte[] ConvertToJpeg(byte[] bgraData, int width, int height)
    {
        var info = new SkiaSharp.SKImageInfo(width, height,
            SkiaSharp.SKColorType.Bgra8888,
            SkiaSharp.SKAlphaType.Premul);

        var handle = System.Runtime.InteropServices.GCHandle.Alloc(
            bgraData, System.Runtime.InteropServices.GCHandleType.Pinned);

        try
        {
            var ptr = handle.AddrOfPinnedObject();
            using var skBitmap = new SkiaSharp.SKBitmap(info);
            skBitmap.InstallPixels(info, ptr, info.RowBytes);

            using var image = SkiaSharp.SKImage.FromBitmap(skBitmap);
            using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Jpeg, 80);
            return data.ToArray();
        }
        finally
        {
            handle.Free();
        }
    }

    public void Dispose() => Stop();
}
