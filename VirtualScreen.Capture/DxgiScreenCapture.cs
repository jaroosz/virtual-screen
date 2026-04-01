using System.Runtime.InteropServices;
using VirtualScreen.Core;

namespace VirtualScreen.Capture;

public class DxgiScreenCapture : IScreenCapture, IDisposable
{
    private Thread? _captureThread;
    private CancellationTokenSource? _cts;

    public bool IsCapturing { get; private set; }
    public event EventHandler<FrameCapturedEventArgs>? FrameCaptured;

    public void Start(string monitorDeviceName)
    {
        if (IsCapturing) return;

        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        _captureThread = new Thread(() => CaptureLoop(monitorDeviceName, token));
        _captureThread.SetApartmentState(ApartmentState.MTA);
        _captureThread.IsBackground = true;
        _captureThread.Start();

        IsCapturing = true;
    }

    public void Stop()
    {
        if (!IsCapturing) return;

        _cts?.Cancel();
        _captureThread?.Join(3000);

        _cts = null;
        _captureThread = null;
        IsCapturing = false;
    }

    private void CaptureLoop(string monitorDeviceName, CancellationToken token)
    {
        // 1. Znajdź output i adapter dla danego monitora
        var (dxgiOutputPtr, adapterPtr) = GetDxgiOutput(monitorDeviceName);
        if (dxgiOutputPtr == IntPtr.Zero)
            throw new Exception($"Nie znaleziono DXGI output dla: {monitorDeviceName}");

        // 2. QI do IDXGIOutput1
        var iidOutput1 = new Guid("00cddea8-939b-4b83-a340-a685226666cc");
        Marshal.QueryInterface(dxgiOutputPtr, ref iidOutput1, out var output1Ptr);
        Marshal.Release(dxgiOutputPtr);

        if (output1Ptr == IntPtr.Zero)
            throw new Exception("IDXGIOutput1 nie jest dostępne.");

        // 3. Stwórz device na tym samym adapterze co output
        var hr = NativeDxgi.D3D11CreateDevice(
            adapterPtr, 0, IntPtr.Zero, 0x20,
            IntPtr.Zero, 0, 7,
            out var devicePtr,
            IntPtr.Zero,
            out var contextPtr);

        Marshal.Release(adapterPtr);

        if (hr != 0)
            throw new Exception($"D3D11CreateDevice failed: 0x{hr:X8}");

        // 4. DuplicateOutput
        var vtable = Marshal.ReadIntPtr(output1Ptr);
        var dupFn = Marshal.ReadIntPtr(vtable, 22 * IntPtr.Size);
        var duplicate = Marshal.GetDelegateForFunctionPointer<NativeDxgi.DuplicateOutputDelegate>(dupFn);

        hr = duplicate(output1Ptr, devicePtr, out var duplicationPtr);
        Marshal.Release(output1Ptr);

        if (hr != 0)
            throw new Exception($"DuplicateOutput failed: 0x{hr:X8}");

        try
        {
            RunFrameLoop(duplicationPtr, devicePtr, contextPtr, token);
        }
        finally
        {
            Marshal.Release(duplicationPtr);
            Marshal.Release(contextPtr);
            Marshal.Release(devicePtr);
        }
    }

    private void RunFrameLoop(
        IntPtr duplication,
        IntPtr devicePtr,
        IntPtr contextPtr,
        CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var hr = NativeDxgi.AcquireNextFrame(duplication, 100, out var frameInfo, out var desktopResource);

            if (hr == unchecked((int)0x887A0027)) // DXGI_ERROR_WAIT_TIMEOUT
                continue;

            if (hr != 0)
                break; // DXGI_ERROR_ACCESS_LOST lub inny błąd

            try
            {
                var iidTex = new Guid("6f15aaf2-d208-4e89-9ab4-489535d34f9c");
                Marshal.QueryInterface(desktopResource, ref iidTex, out var texturePtr);
                Marshal.Release(desktopResource);

                if (texturePtr == IntPtr.Zero) continue;

                try
                {
                    NativeDxgi.GetTextureDesc(texturePtr, out var width, out var height);

                    var stagingPtr = NativeDxgi.CreateStagingTexture(devicePtr, width, height);

                    if (stagingPtr == IntPtr.Zero) continue;

                    try
                    {
                        NativeDxgi.CopyResource(contextPtr, stagingPtr, texturePtr);
                        var bytes = NativeDxgi.ReadTextureBytes(contextPtr, stagingPtr, width, height);

                        if (bytes != null)
                        {
                            FrameCaptured?.Invoke(this, new FrameCapturedEventArgs
                            {
                                Data = bytes,
                                Width = width,
                                Height = height,
                                Timestamp = DateTime.UtcNow
                            });
                        }
                    }
                    finally
                    {
                        Marshal.Release(stagingPtr);
                    }
                }
                finally
                {
                    Marshal.Release(texturePtr);
                }
            }
            finally
            {
                NativeDxgi.ReleaseFrame(duplication);
            }
        }
    }

    private static (IntPtr output, IntPtr adapter) GetDxgiOutput(string monitorDeviceName)
    {
        var iidFactory = new Guid("770aae78-f26f-4dba-a829-253c83d1b387");
        var hr = NativeDxgi.CreateDXGIFactory1(ref iidFactory, out var factoryPtr);
        if (hr != 0) throw new Exception($"CreateDXGIFactory1 failed: 0x{hr:X8}");

        uint adapterIndex = 0;
        while (true)
        {
            var factoryVtable = Marshal.ReadIntPtr(factoryPtr);
            var enumAdaptersFn = Marshal.ReadIntPtr(factoryVtable, 12 * IntPtr.Size);
            var enumAdapters = Marshal.GetDelegateForFunctionPointer<NativeDxgi.EnumAdaptersDelegate>(enumAdaptersFn);

            var result = enumAdapters(factoryPtr, adapterIndex, out var adapterPtr);
            if (result != 0) break; // DXGI_ERROR_NOT_FOUND

            uint outputIndex = 0;
            while (true)
            {
                var adapterVtable = Marshal.ReadIntPtr(adapterPtr);
                var enumOutputsFn = Marshal.ReadIntPtr(adapterVtable, 7 * IntPtr.Size);
                var enumOutputs = Marshal.GetDelegateForFunctionPointer<NativeDxgi.EnumOutputsDelegate>(enumOutputsFn);

                var outResult = enumOutputs(adapterPtr, outputIndex, out var outputPtr);
                if (outResult != 0) break;

                var outVtable = Marshal.ReadIntPtr(outputPtr);
                var getDescFn = Marshal.ReadIntPtr(outVtable, 7 * IntPtr.Size);
                var getDesc = Marshal.GetDelegateForFunctionPointer<NativeDxgi.GetOutputDescDelegate>(getDescFn);
                getDesc(outputPtr, out var desc);

                var name = new string(desc.DeviceName).TrimEnd('\0');
                Console.WriteLine($"DXGI output: {name}");

                if (name.Equals(monitorDeviceName, StringComparison.OrdinalIgnoreCase))
                {
                    // Nie zwalniamy adapterPtr — zwracamy go razem z outputem
                    Marshal.Release(factoryPtr);
                    return (outputPtr, adapterPtr);
                }

                Marshal.Release(outputPtr);
                outputIndex++;
            }

            Marshal.Release(adapterPtr);
            adapterIndex++;
        }

        Marshal.Release(factoryPtr);
        return (IntPtr.Zero, IntPtr.Zero);
    }

    public void Dispose() => Stop();
}