namespace VirtualScreenViewer.Services;

public interface IVideoDecoder : IDisposable
{
    void Initialize(int width, int height);
    byte[]? DecodeFrame(byte[] h265Data, uint frameNumber = 0);
    event EventHandler<DecodedFrameEventArgs>? FrameDecoded;
}

public class DecodedFrameEventArgs : EventArgs
{
    public byte[] RgbaData { get; init; } = Array.Empty<byte>();
    public int Width { get; init; }
    public int Height { get; init; }
    public long Timestamp { get; init; }
    public int FrameNumber { get; init; }
}