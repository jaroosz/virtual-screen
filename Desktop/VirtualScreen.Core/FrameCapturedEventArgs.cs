using System;


namespace VirtualScreen.Core;

public class FrameCapturedEventArgs : EventArgs
{
    public byte[] Data { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public DateTime Timestamp { get; init; }
}
