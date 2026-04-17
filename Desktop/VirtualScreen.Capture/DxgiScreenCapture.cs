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
 
    private const double TargetFrameMs = 1000.0 / 60.0;
 
    private long _lastMouseUpdateTime;
    private long _lastEmitTick;
 
    private bool _pendingCursorChanged;
    private int _pendingCursorX;
    private int _pendingCursorY;
    private bool _pendingCursorVisible;

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
        var sw = Stopwatch.StartNew();
        // int frames = 0;
        long ticksPerMs = Stopwatch.Frequency / 1200L;
        long frameIntervalTicks = (long)(TargetFrameMs * ticksPerMs);

        while (!token.IsCancellationRequested)
        {
            var hr = NativeDxgi.AcquireNextFrame(duplication, 100, out var frameInfo, out var desktopResource);

            if (hr == unchecked((int)0x887A0027))
                continue;

            if (hr != 0)
                break;

            try
            {
                if (frameInfo.LastMouseUpdateTime != _lastMouseUpdateTime)
                {
                    _lastMouseUpdateTime = frameInfo.LastMouseUpdateTime;
                    var pos = frameInfo.PointerPosition;
                    _pendingCursorChanged = true;
                    _pendingCursorX = pos.X;
                    _pendingCursorY = pos.Y;
                    _pendingCursorVisible = pos.Visible;
                }

                var now = Stopwatch.GetTimestamp();
                if (now - _lastEmitTick < frameIntervalTicks)
                {
                    if (desktopResource != IntPtr.Zero)
                        Marshal.Release(desktopResource);
                    continue;
                }

                _lastEmitTick = now;

                //frames++;
                //var elapsedMs = sw.ElapsedMilliseconds;
                //if (elapsedMs >= 1000)
                //{
                //    Console.WriteLine($"[Capture] FPS: {frames / (elapsedMs / 1000.0):F1}");
                //    frames = 0;
                //    sw.Restart();
                //}

                if (desktopResource == IntPtr.Zero)
                {
                    if (!_pendingCursorChanged) continue;

                    TextureCaptured?.Invoke(this, new TextureCapturedEventArgs
                    {
                        TexturePtr = 0,
                        DevicePtr = devicePtr,
                        ContextPtr = contextPtr,
                        Width = 0,
                        Height = 0,
                        CursorMoved = true,
                        CursorX = _pendingCursorX,
                        CursorY = _pendingCursorY,
                        CursorVisible = _pendingCursorVisible,
                        Timestamp = DateTime.UtcNow
                    });
                    _pendingCursorChanged = false;
                    continue;
                }

                var iidTex = new Guid("6f15aaf2-d208-4e89-9ab4-489535d34f9c");
                Marshal.QueryInterface(desktopResource, ref iidTex, out var texturePtr);
                Marshal.Release(desktopResource);

                if (texturePtr == IntPtr.Zero) continue;

                try
                {
                    NativeDxgi.GetTextureDesc(texturePtr, out var width, out var height);

                    TextureCaptured?.Invoke(this, new TextureCapturedEventArgs
                    {
                        TexturePtr = texturePtr,
                        DevicePtr = devicePtr,
                        ContextPtr = contextPtr,
                        Width = width,
                        Height = height,
                        CursorMoved = _pendingCursorChanged,
                        CursorX = _pendingCursorX,
                        CursorY = _pendingCursorY,
                        CursorVisible = _pendingCursorVisible,
                        Timestamp = DateTime.UtcNow
                    });
                    _pendingCursorChanged = false;
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

        // try without preferring NVIDIA
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
                // Get adapter description to check vendor
                var adapterVtable = Marshal.ReadIntPtr(adapterPtr);
                var getDescFn = Marshal.ReadIntPtr(adapterVtable, 8 * IntPtr.Size);
                var getAdapterDesc = Marshal.GetDelegateForFunctionPointer<NativeDxgi.GetAdapterDescDelegate>(getDescFn);
                getAdapterDesc(adapterPtr, out var adapterDesc);

                // VendorId: 0x10DE = NVIDIA, 0x8086 = Intel, 0x1002 = AMD
                bool isNvidia = adapterDesc.VendorId == 0x10DE;

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