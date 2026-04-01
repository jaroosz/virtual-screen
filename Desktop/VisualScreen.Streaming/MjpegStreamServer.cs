using System.Net;
using System.Text;
using VirtualScreen.Core;

namespace VirtualScreen.Streaming;

public class MjpegStreamServer : IStreamServer
{
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private byte[]? _currentFrame;
    private readonly Lock _frameLock = new();
    private Task? _serverTask;
    private int _width;
    private int _height;

    public bool IsRunning { get; private set; }
    public int Port { get; private set; }

    public void Start(int port)
    {
        if (IsRunning) return;

        Port = port;
        _cts = new CancellationTokenSource();

        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://*:{port}/");
        _listener.Start();

        _serverTask = Task.Run(() => ListenLoop(_cts.Token));

        IsRunning = true;
    }

    public void Stop()
    {
        if (!IsRunning) return;

        _cts?.Cancel();
        _listener?.Stop();
        _listener?.Close();

        _cts = null;
        _listener = null;
        IsRunning = false;
    }

    public void SendFrame(byte[] frameData, int width, int height)
    {
        _width = width;
        _height = height;

        var jpeg = ConvertToJpeg(frameData);

        lock (_frameLock)
        {
            _currentFrame = jpeg;
        }
    }

    private async Task ListenLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var context = await _listener!.GetContextAsync();

                _ = Task.Run(() => HandleClient(context, ct), ct);
            }
            catch (Exception) when (ct.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task HandleClient(HttpListenerContext context, CancellationToken ct)
    {
        var response = context.Response;

        response.ContentType = "multipart/x-mixed-replace; boundary=frame";
        response.StatusCode = 200;

        try
        {
            while (!ct.IsCancellationRequested)
            {
                byte[]? frame;
                lock (_frameLock)
                {
                    frame = _currentFrame;
                }

                if (frame != null)
                {
                    var header = Encoding.ASCII.GetBytes(
                        "--frame\r\n" +
                        "Content-Type: image/jpeg\r\n" +
                        $"Content-Length: {frame.Length}\r\n\r\n");

                    await response.OutputStream.WriteAsync(header, ct);
                    await response.OutputStream.WriteAsync(frame, ct);
                    await response.OutputStream.WriteAsync(
                        Encoding.ASCII.GetBytes("\r\n"), ct);
                    await response.OutputStream.FlushAsync(ct);
                }

                await Task.Delay(33, ct);
            }
        }
        catch (Exception)
        {

        }
        finally
        {
            response.Close();
        }
    }

    private byte[] ConvertToJpeg(byte[] bgraData)
    {
        using var bitmap = new SkiaSharp.SKBitmap();
        var info = new SkiaSharp.SKImageInfo(_width, _height,
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

    public void Dispose()
    {
        Stop();
    }
}
