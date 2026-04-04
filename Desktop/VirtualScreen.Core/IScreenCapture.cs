using System;

namespace VirtualScreen.Core;

public interface IScreenCapture
{
    bool IsCapturing { get; }
    void Start(string monitorDeviceName);
    void Stop();
    event EventHandler<TextureCapturedEventArgs> TextureCaptured;
}

public class TextureCapturedEventArgs : EventArgs
{
    public IntPtr TexturePtr { get; init; }
    public IntPtr DevicePtr { get; init; }
    public IntPtr ContextPtr { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public DateTime Timestamp { get; init; }
}
