using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using VirtualScreen.Core;
using VirtualScreen.Core.Interface;

namespace VirtualScreen.Capture;

public class WGCScreenCapture : IScreenCapture, IDisposable
{
    private Thread? _captureThread;
    private CancellationTokenSource? _cts;

    private const int TargetFps = 60;

    public bool IsCapturing { get; private set; }
    public event EventHandler<TextureCapturedEventArgs>? TextureCaptured;

    public void Start(string monitorDeviceName)
    {
        if (IsCapturing) return;
        _cts = new CancellationTokenSource();

        _captureThread = new Thread(() => CaptureSession(monitorDeviceName, _cts.Token))
        {
            IsBackground = true
        };

        _captureThread.SetApartmentState(ApartmentState.STA);

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

    private void CaptureSession(string monitorDeviceName, CancellationToken token)
    {
        var monitor = MonitorHelper.GetMonitors()
            .FirstOrDefault(m => m.DeviceName.Equals(monitorDeviceName, StringComparison.OrdinalIgnoreCase));

        if (monitor == null)
            throw new Exception($"Monitor not found: {monitorDeviceName}");

        var nvidiaAdapterPtr = GetNvidiaAdapter();

        var hr = NativeD3D.D3D11CreateDevice(
            nvidiaAdapterPtr, 0, IntPtr.Zero, 0x20,
            IntPtr.Zero, 0, 7,
            out var devicePtr, IntPtr.Zero, out var contextPtr);

        Marshal.Release(nvidiaAdapterPtr);

        if (hr != 0)
            throw new Exception($"D3D11CreateDevice failed: 0x{hr:X8}");

        try
        {
            var iidDxgi = new Guid("54ec77fa-1377-44e6-8c32-88fd5f44c84c");
            Marshal.QueryInterface(devicePtr, ref iidDxgi, out var dxgiDevicePtr);

            hr = CreateDirect3D11DeviceFromDXGIDevice(dxgiDevicePtr, out var winrtDevicePtr);
            Marshal.Release(dxgiDevicePtr);

            if (hr != 0)
                throw new Exception($"CreateDirect3D11DeviceFromDXGIDevice failed: 0x{hr:X8}");

            var winrtDevice = WinRT.MarshalInterface<IDirect3DDevice>.FromAbi(winrtDevicePtr);
            Marshal.Release(winrtDevicePtr);

            var factoryPtr = WinRT.ActivationFactory.Get("Windows.Graphics.Capture.GraphicsCaptureItem").ThisPtr;
            var interop = (Interface.IGraphicsCaptureItemInterop)Marshal.GetObjectForIUnknown(factoryPtr);
            var iidItem = new Guid("79C3F95B-31F7-4EC2-A464-632EF5D30760");

            interop.CreateForMonitor(monitor.HMonitor, ref iidItem, out var itemPtr);
            var item = GraphicsCaptureItem.FromAbi(itemPtr);
            Marshal.Release(itemPtr);

            using var framePool = Direct3D11CaptureFramePool.Create(
                winrtDevice,
                DirectXPixelFormat.B8G8R8A8UIntNormalized,
                2,
                item.Size);

            using var session = framePool.CreateCaptureSession(item);
            session.IsCursorCaptureEnabled = false;
            session.StartCapture();

            RunFrameLoop(framePool, devicePtr, contextPtr, monitor, token);

            session.Dispose();
        }
        finally
        {
            Marshal.Release(contextPtr);
            Marshal.Release(devicePtr);
        }
    }

    private void RunFrameLoop(
        Direct3D11CaptureFramePool framePool,
        IntPtr devicePtr,
        IntPtr contextPtr,
        MonitorHelper.MonitorInfo monitor,
        CancellationToken token)
    {
        var iidTexture = new Guid("6f15aaf2-d208-4e89-9ab4-489535d34f9c");
        var iidDxgiAccess = new Guid("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1");
        const uint PM_REMOVE = 1;

        long intervalTicks = Stopwatch.Frequency / TargetFps;
        long nextTick = Stopwatch.GetTimestamp();

        while (!token.IsCancellationRequested)
        {
            while (PeekMessage(out var msg, IntPtr.Zero, 0, 0, PM_REMOVE))
            {
                TranslateMessage(ref msg);
                DispatchMessage(ref msg);
            }

            while (Stopwatch.GetTimestamp() < nextTick)
                Thread.SpinWait(10);
            nextTick += intervalTicks;

            using var frame = framePool.TryGetNextFrame();
            if (frame == null) continue;

            var surfacePtr = ((WinRT.IWinRTObject)frame.Surface).NativeObject.ThisPtr;
            Marshal.QueryInterface(surfacePtr, ref iidDxgiAccess, out var dxgiAccessPtr);
            if (dxgiAccessPtr == IntPtr.Zero) continue;

            var dxgiAccess = (IDirect3DDxgiInterfaceAccess)Marshal.GetObjectForIUnknown(dxgiAccessPtr);
            dxgiAccess.GetInterface(ref iidTexture, out var texturePtr);
            Marshal.Release(dxgiAccessPtr);

            if (texturePtr == IntPtr.Zero) continue;

            try
            {
                var (cx, cy, cursorType) = MonitorHelper.GetCursorInfo(
                    monitor.X, monitor.Y,
                    frame.ContentSize.Width, frame.ContentSize.Height);

                var args = new TextureCapturedEventArgs
                {
                    TexturePtr = texturePtr,
                    DevicePtr = devicePtr,
                    ContextPtr = contextPtr,
                    Width = frame.ContentSize.Width,
                    Height = frame.ContentSize.Height,
                    CursorX = cx,
                    CursorY = cy,
                    CursorVisible = cursorType != VirtualScreen.Core.Protocol.CursorType.Hidden,
                    Timestamp = DateTime.UtcNow
                };

                TextureCaptured?.Invoke(this, args);
            }
            finally
            {
                Marshal.Release(texturePtr);
            }
        }
    }

    private static IntPtr GetNvidiaAdapter()
    {
        var iidFactory = new Guid("770aae78-f26f-4dba-a829-253c83d1b387");
        var hr = NativeD3D.CreateDXGIFactory1(ref iidFactory, out var factoryPtr);
        if (hr != 0) throw new Exception($"CreateDXGIFactory1 failed: 0x{hr:X8}");

        uint adapterIndex = 0;
        while (true)
        {
            var factoryVtable = Marshal.ReadIntPtr(factoryPtr);
            var enumAdaptersFn = Marshal.ReadIntPtr(factoryVtable, 12 * IntPtr.Size);
            var enumAdapters = Marshal.GetDelegateForFunctionPointer<NativeD3D.EnumAdaptersDelegate>(enumAdaptersFn);

            var result = enumAdapters(factoryPtr, adapterIndex, out var adapterPtr);
            if (result != 0) break;

            var adapterVtable = Marshal.ReadIntPtr(adapterPtr);
            var getDescFn = Marshal.ReadIntPtr(adapterVtable, 8 * IntPtr.Size);
            var getAdapterDesc = Marshal.GetDelegateForFunctionPointer<NativeD3D.GetAdapterDescDelegate>(getDescFn);
            getAdapterDesc(adapterPtr, out var adapterDesc);

            if (adapterDesc.VendorId == 0x10DE) // NVIDIA
            {
                Marshal.Release(factoryPtr);
                return adapterPtr;
            }

            Marshal.Release(adapterPtr);
            adapterIndex++;
        }

        Marshal.Release(factoryPtr);
        throw new Exception("NVIDIA adapter not found");
    }

    [ComImport]
    [Guid("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IDirect3DDxgiInterfaceAccess
    {
        void GetInterface(ref Guid iid, out IntPtr p);
    }

    [DllImport("user32.dll")]
    private static extern bool PeekMessage(out MSG msg, IntPtr hwnd, uint min, uint max, uint remove);

    [DllImport("user32.dll")]
    private static extern bool TranslateMessage([In] ref MSG lpMsg);

    [DllImport("user32.dll")]
    private static extern IntPtr DispatchMessage([In] ref MSG lpmsg);

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG { public IntPtr hwnd, wParam, lParam; public uint message, time; public int x, y; }

    [DllImport("d3d11.dll", ExactSpelling = true)]
    private static extern int CreateDirect3D11DeviceFromDXGIDevice(IntPtr dxgiDevice, out IntPtr d3dDevice);

    public void Dispose() => Stop();
}