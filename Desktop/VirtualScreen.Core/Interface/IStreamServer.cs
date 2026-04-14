using System;

namespace VirtualScreen.Core.Interface;

public interface IStreamServer : IDisposable
{
    bool IsRunning { get; }
    int Port { get; }
    void Start(int port);
    void Stop();
}
