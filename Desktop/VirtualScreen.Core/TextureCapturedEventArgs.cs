namespace VirtualScreen.Core;

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
    public int CursorX { get; init; }
    public int CursorY { get; init; }
    public bool CursorVisible { get; init; }

    public DateTime Timestamp { get; init; }
}