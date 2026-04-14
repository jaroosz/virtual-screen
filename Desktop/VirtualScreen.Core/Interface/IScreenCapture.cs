using System;

namespace VirtualScreen.Core.Interface;

public interface IScreenCapture
{
    bool IsCapturing { get; }
    void Start(string monitorDeviceName);
    void Stop();
    event EventHandler<TextureCapturedEventArgs> TextureCaptured;
}

public class TextureCapturedEventArgs : EventArgs
{
    public nint TexturePtr { get; init; }
    public nint DevicePtr { get; init; }
    public nint ContextPtr { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public DateTime Timestamp { get; init; }
}
