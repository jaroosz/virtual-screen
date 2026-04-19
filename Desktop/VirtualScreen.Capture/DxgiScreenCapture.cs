using System.Runtime.InteropServices;
using System.Diagnostics;
using VirtualScreen.Core.Interface;

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

        _captureThread = new Thread(() => CaptureLoop(monitorDeviceName, _cts.Token))
        {
            ApartmentState = ApartmentState.MTA,
            IsBackground = true
        };

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
        var normalized = NormalizeDeviceName(monitorDeviceName);
        var (dxgiOutputPtr, adapterPtr) = GetDxgiOutput(normalized);
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

    private void RunFrameLoop(IntPtr duplication, IntPtr devicePtr, IntPtr contextPtr, CancellationToken token)
    {
        long intervalTicks = Stopwatch.Frequency / 60L;
        long nextTick = Stopwatch.GetTimestamp();

        while (!token.IsCancellationRequested)
        {
            while (Stopwatch.GetTimestamp() < nextTick)
                Thread.SpinWait(10);
            nextTick += intervalTicks;

            var hr = NativeDxgi.AcquireNextFrame(duplication, 0, out var frameInfo, out var desktopResource);

            if (hr == unchecked((int)0x887A0027)) continue;
            if (hr != 0)
            {
                Console.WriteLine($"[Capture] error: 0x{hr:X8}");
                break;
            }

            try
            {
                var pos = frameInfo.PointerPosition;

                IntPtr texturePtr = IntPtr.Zero;
                int width = 0, height = 0;

                if (desktopResource != IntPtr.Zero)
                {
                    var iidTex = new Guid("6f15aaf2-d208-4e89-9ab4-489535d34f9c");
                    Marshal.QueryInterface(desktopResource, ref iidTex, out texturePtr);
                    if (texturePtr != IntPtr.Zero)
                        NativeDxgi.GetTextureDesc(texturePtr, out width, out height);
                }

                try
                {
                    TextureCaptured?.Invoke(this, new TextureCapturedEventArgs
                    {
                        TexturePtr = texturePtr,
                        DevicePtr = devicePtr,
                        ContextPtr = contextPtr,
                        Width = width,
                        Height = height,
                        CursorX = pos.X,
                        CursorY = pos.Y,
                        CursorVisible = pos.Visible,
                        Timestamp = DateTime.UtcNow
                    });
                }
                finally
                {
                    if (texturePtr != IntPtr.Zero)
                        Marshal.Release(texturePtr);
                }
            }
            finally
            {
                NativeDxgi.ReleaseFrame(duplication);
            }
        }
    }

    private static (IntPtr output, IntPtr adapter) GetDxgiOutput(string? monitorDeviceName)
    {
        var iidFactory = new Guid("770aae78-f26f-4dba-a829-253c83d1b387");
        var hr = NativeDxgi.CreateDXGIFactory1(ref iidFactory, out var factoryPtr);
        if (hr != 0) throw new Exception($"CreateDXGIFactory1 failed: 0x{hr:X8}");

        var result = TryFindAdapterOutput(factoryPtr, monitorDeviceName, preferNvidia: true);
        if (result.output != IntPtr.Zero)
        {
            Marshal.Release(factoryPtr);
            return result;
        }

        result = TryFindAdapterOutput(factoryPtr, monitorDeviceName, preferNvidia: false);
        Marshal.Release(factoryPtr);

        return result;
    }

    private static (IntPtr output, IntPtr adapter) TryFindAdapterOutput(
        IntPtr factoryPtr,
        string? targetDeviceName,
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
                var adapterVtable = Marshal.ReadIntPtr(adapterPtr);
                var getDescFn = Marshal.ReadIntPtr(adapterVtable, 8 * IntPtr.Size);
                var getAdapterDesc = Marshal.GetDelegateForFunctionPointer<NativeDxgi.GetAdapterDescDelegate>(getDescFn);
                getAdapterDesc(adapterPtr, out var adapterDesc);

                bool isNvidia = adapterDesc.VendorId == 0x10DE;

                if (preferNvidia && !isNvidia)
                {
                    Marshal.Release(adapterPtr);
                    adapterIndex++;
                    continue;
                }

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

                    if (string.IsNullOrEmpty(targetDeviceName) ||
                        name.Equals(targetDeviceName, StringComparison.OrdinalIgnoreCase) ||
                        name.EndsWith(targetDeviceName, StringComparison.OrdinalIgnoreCase))
                    {
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

    private static string? NormalizeDeviceName(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var s = input.Trim();

        if (int.TryParse(s, out var index) && index > 0)
            return $@"\\.\DISPLAY{index}";

        if (s.StartsWith("DISPLAY", StringComparison.OrdinalIgnoreCase))
            return $@"\\.\{s.ToUpperInvariant()}";

        if (s.StartsWith(@"\\.\DISPLAY", StringComparison.OrdinalIgnoreCase))
            return s;

        return s;
    }

    public void Dispose() => Stop();
}