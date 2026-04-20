namespace VirtualScreen.Core.Interface;

public interface IScreenCapture
{
    bool IsCapturing { get; }
    void Start(string monitorDeviceName);
    void Stop();
    event EventHandler<TextureCapturedEventArgs> TextureCaptured;
}