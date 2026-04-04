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

    private bool _testMode = true;
    private int _encodedFrameCount = 0;
    private long _totalEncodedBytes = 0;

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
        if (!_testMode && (_clientEndpoint == null || _udpServer == null))
        {
            return;
        }

        try
        {
            if (_encoder == null)
            {
                _encoder = new NvencH265Encoder(e.DevicePtr, e.Width, e.Height, bitrate: 15_000_000);
                Console.WriteLine($"[NVENC] Encoder initialized for {e.Width}x{e.Height}");
            }

            var result = _encoder.EncodeTexture(e.TexturePtr);

            if (result == null) return;

            var (buffer, length) = result.Value;

            _encodedFrameCount++;
            _totalEncodedBytes += length;

            if (_encodedFrameCount % 60 == 0)
            {
                var avgSize = _totalEncodedBytes / _encodedFrameCount;
                Console.WriteLine($"[NVENC] Frame #{_encodedFrameCount}: {length:N0} bytes (avg: {avgSize:N0} bytes/frame)");
            }

            try
            {
                var h265Data = buffer.AsSpan(0, length).ToArray();
                var fragments = StreamPacket.CreateFragments(h265Data, e.Width, e.Height, _sequenceNumber++);

                if (_clientEndpoint != null && _udpServer != null)
                {
                    foreach (var fragment in fragments)
                    {
                        var data = fragment.ToBytes();
                        _udpServer.Send(data, data.Length, _clientEndpoint);
                    }
                }
                else if (_testMode && _encodedFrameCount == 1)
                {
                    Console.WriteLine($"[UDP] Would send {fragments.Count} fragment(s) (no client connected)");
                }
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
