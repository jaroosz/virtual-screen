using System;

namespace VirtualScreen.Core;

public interface IStreamServer : IDisposable
{
    bool IsRunning { get; }
    int Port { get; }
    void Start(int port);
    void Stop();
}
