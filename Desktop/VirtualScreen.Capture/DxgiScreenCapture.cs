using System.Runtime.InteropServices;
using VirtualScreen.Core;

namespace VirtualScreen.Capture;

public class DxgiScreenCapture : IScreenCapture, IDisposable
{
    private Thread? _captureThread;
    private CancellationTokenSource? _cts;

    public bool IsCapturing { get; private set; }

    public event EventHandler<TextureCapturedEventArgs>? TextureCaptured;

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
        var (dxgiOutputPtr, adapterPtr) = GetDxgiOutput(monitorDeviceName);
        if (dxgiOutputPtr == IntPtr.Zero)
            throw new Exception($"Cannot find DXGI output for: {monitorDeviceName}");

        var iidOutput1 = new Guid("00cddea8-939b-4b83-a340-a685226666cc");
        Marshal.QueryInterface(dxgiOutputPtr, ref iidOutput1, out var output1Ptr);
        Marshal.Release(dxgiOutputPtr);

        if (output1Ptr == IntPtr.Zero)
            throw new Exception("IDXGIOutput1 is not accessible.");

        var hr = NativeDxgi.D3D11CreateDevice(
            adapterPtr, 0, IntPtr.Zero, 0x20,
            IntPtr.Zero, 0, 7,
            out var devicePtr,
            IntPtr.Zero,
            out var contextPtr);

        Marshal.Release(adapterPtr);

        if (hr != 0)
            throw new Exception($"D3D11CreateDevice failed: 0x{hr:X8}");

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
                break;

            try
            {
                var iidTex = new Guid("6f15aaf2-d208-4e89-9ab4-489535d34f9c");
                Marshal.QueryInterface(desktopResource, ref iidTex, out var texturePtr);
                Marshal.Release(desktopResource);

                if (texturePtr == IntPtr.Zero) continue;

                try
                {
                    NativeDxgi.GetTextureDesc(texturePtr, out var width, out var height);

                    if (TextureCaptured != null && TextureCaptured.GetInvocationList().Length > 0)
                    {
                        TextureCaptured.Invoke(this, new TextureCapturedEventArgs
                        {
                            TexturePtr = texturePtr,
                            DevicePtr = devicePtr,
                            ContextPtr = contextPtr,
                            Width = width,
                            Height = height,
                            Timestamp = DateTime.UtcNow
                        });
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

        var result = TryFindAdapterOutput(factoryPtr, monitorDeviceName, preferNvidia: true);
        if (result.output != IntPtr.Zero)
        {
            Marshal.Release(factoryPtr);
            Console.WriteLine("Using NVIDIA adapter");
            return result;
        }

        Console.WriteLine("NVIDIA adapter not found");
        result = TryFindAdapterOutput(factoryPtr, monitorDeviceName, preferNvidia: false);
        Marshal.Release(factoryPtr);

        return result;
    }

    private static (IntPtr output, IntPtr adapter) TryFindAdapterOutput(
    IntPtr factoryPtr,
    string monitorDeviceName,
    bool preferNvidia)
    {
        uint adapterIndex = 0;
        while (true)
        {
            var factoryVtable = Marshal.ReadIntPtr(factoryPtr);
            var enumAdaptersFn = Marshal.ReadIntPtr(factoryVtable, 12 * IntPtr.Size);
            var enumAdapters = Marshal.GetDelegateForFunctionPointer<NativeDxgi.EnumAdaptersDelegate>(enumAdaptersFn);

            var result = enumAdapters(factoryPtr, adapterIndex, out var adapterPtr);
            if (result != 0) break;

            try
            {
                // Get adapter description to check vendor
                var adapterVtable = Marshal.ReadIntPtr(adapterPtr);
                var getDescFn = Marshal.ReadIntPtr(adapterVtable, 8 * IntPtr.Size);
                var getAdapterDesc = Marshal.GetDelegateForFunctionPointer<NativeDxgi.GetAdapterDescDelegate>(getDescFn);
                getAdapterDesc(adapterPtr, out var adapterDesc);

                // VendorId: 0x10DE = NVIDIA, 0x8086 = Intel, 0x1002 = AMD
                bool isNvidia = adapterDesc.VendorId == 0x10DE;
                string vendorName = adapterDesc.VendorId switch
                {
                    0x10DE => "NVIDIA",
                    0x8086 => "Intel",
                    0x1002 => "AMD",
                    _ => $"Unknown (0x{adapterDesc.VendorId:X4})"
                };

                Console.WriteLine($"Adapter {adapterIndex}: {vendorName} (VendorID: 0x{adapterDesc.VendorId:X4})");

                // If we're filtering for NVIDIA and this isn't NVIDIA, skip it
                if (preferNvidia && !isNvidia)
                {
                    Marshal.Release(adapterPtr);
                    adapterIndex++;
                    continue;
                }

                // Try to find the monitor on this adapter
                uint outputIndex = 0;
                while (true)
                {
                    var enumOutputsFn = Marshal.ReadIntPtr(adapterVtable, 7 * IntPtr.Size);
                    var enumOutputs = Marshal.GetDelegateForFunctionPointer<NativeDxgi.EnumOutputsDelegate>(enumOutputsFn);

                    var outResult = enumOutputs(adapterPtr, outputIndex, out var outputPtr);
                    if (outResult != 0) break;

                    var outVtable = Marshal.ReadIntPtr(outputPtr);
                    var getOutputDescFn = Marshal.ReadIntPtr(outVtable, 7 * IntPtr.Size);
                    var getOutputDesc = Marshal.GetDelegateForFunctionPointer<NativeDxgi.GetOutputDescDelegate>(getOutputDescFn);
                    getOutputDesc(outputPtr, out var outputDesc);

                    var name = new string(outputDesc.DeviceName).TrimEnd('\0');
                    Console.WriteLine($"Output {outputIndex}: {name}");

                    if (name.Equals(monitorDeviceName, StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"Match found on {vendorName} adapter!");
                        return (outputPtr, adapterPtr);
                    }

                    Marshal.Release(outputPtr);
                    outputIndex++;
                }

                Marshal.Release(adapterPtr);
            }
            catch
            {
                Marshal.Release(adapterPtr);
                throw;
            }

            adapterIndex++;
        }

        return (IntPtr.Zero, IntPtr.Zero);
    }

    public void Dispose() => Stop();
}