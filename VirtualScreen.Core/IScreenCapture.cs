using System;

namespace VirtualScreen.Core;

public interface IScreenCapture
{
    bool IsCapturing { get; }
    void Start(string monitorDeviceName);
    void Stop();
    event EventHandler<FrameCapturedEventArgs> FrameCaptured;
}
