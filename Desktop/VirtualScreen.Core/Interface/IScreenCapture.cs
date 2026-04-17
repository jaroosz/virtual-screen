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
    // pointers. when only cursor moves, ptr may be 0
    public nint TexturePtr { get; init; }
    public nint DevicePtr { get; init; }
    public nint ContextPtr { get; init; }

    // frame size
    public int Width { get; init; }
    public int Height { get; init; }

    // cursor info
    public bool CursorMoved { get; init; }
    public int CursorX { get; init; }
    public int CursorY { get; init; }
    public bool CursorVisible { get; init; }

    public DateTime Timestamp { get; init; }
}
