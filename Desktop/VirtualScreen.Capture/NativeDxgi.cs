using System.Runtime.InteropServices;

namespace VirtualScreen.Capture;

internal static class NativeDxgi
{
    [DllImport("d3d11.dll", ExactSpelling = true)]
    public static extern int D3D11CreateDevice(
        IntPtr pAdapter, uint DriverType, IntPtr Software, uint Flags,
        IntPtr pFeatureLevels, uint FeatureLevels, uint SDKVersion,
        out IntPtr ppDevice, IntPtr pFeatureLevel, out IntPtr ppImmediateContext);

    [DllImport("dxgi.dll", ExactSpelling = true)]
    public static extern int CreateDXGIFactory1(ref Guid riid, out IntPtr ppFactory);

    // ── Delegates ──────────────────────────────────────────────────────────────

    public delegate int EnumOutputsDelegate(IntPtr self, uint output, out IntPtr ppOutput);
    public delegate int GetOutputDescDelegate(IntPtr self, out DXGI_OUTPUT_DESC pDesc);
    public delegate int DuplicateOutputDelegate(IntPtr self, IntPtr pDevice, out IntPtr ppDuplication);
    public delegate int AcquireNextFrameDelegate(IntPtr self, uint timeoutMs, out DXGI_OUTDUPL_FRAME_INFO pInfo, out IntPtr ppDesktopResource);
    public delegate int ReleaseFrameDelegate(IntPtr self);
    public delegate int EnumAdaptersDelegate(IntPtr self, uint index, out IntPtr ppAdapter);
    public delegate int GetAdapterDescDelegate(IntPtr self, out DXGI_ADAPTER_DESC pDesc);

    // ── Wrappers ───────────────────────────────────────────────────────────────

    public static int AcquireNextFrame(IntPtr duplication, uint timeoutMs,
        out DXGI_OUTDUPL_FRAME_INFO frameInfo, out IntPtr desktopResource)
    {
        var vtable = Marshal.ReadIntPtr(duplication);
        // IDXGIOutputDuplication::AcquireNextFrame — metoda nr 8
        var fn = Marshal.ReadIntPtr(vtable, 8 * IntPtr.Size);
        var del = Marshal.GetDelegateForFunctionPointer<AcquireNextFrameDelegate>(fn);
        return del(duplication, timeoutMs, out frameInfo, out desktopResource);
    }

    public static void ReleaseFrame(IntPtr duplication)
    {
        var vtable = Marshal.ReadIntPtr(duplication);
        // IDXGIOutputDuplication::ReleaseFrame — metoda nr 11
        var fn = Marshal.ReadIntPtr(vtable, 14 * IntPtr.Size);
        var del = Marshal.GetDelegateForFunctionPointer<ReleaseFrameDelegate>(fn);
        del(duplication);
    }

    public static void GetTextureDesc(IntPtr texturePtr, out int width, out int height)
    {
        var desc = new int[11];
        var vtable = Marshal.ReadIntPtr(texturePtr);
        var fn = Marshal.ReadIntPtr(vtable, 10 * IntPtr.Size);
        var del = Marshal.GetDelegateForFunctionPointer<GetDescDelegate>(fn);
        del(texturePtr, desc);
        width = desc[0];
        height = desc[1];
    }

    public static IntPtr CreateStagingTexture(IntPtr devicePtr, int width, int height)
    {
        var desc = new D3D11_TEXTURE2D_DESC
        {
            Width = (uint)width,
            Height = (uint)height,
            MipLevels = 1,
            ArraySize = 1,
            Format = 87,
            SampleDescCount = 1,
            SampleDescQuality = 0,
            Usage = 3,
            BindFlags = 0,
            CPUAccessFlags = 0x20000,
            MiscFlags = 0
        };
        var vtable = Marshal.ReadIntPtr(devicePtr);
        var fn = Marshal.ReadIntPtr(vtable, 5 * IntPtr.Size);
        var del = Marshal.GetDelegateForFunctionPointer<CreateTexture2DDelegate>(fn);
        del(devicePtr, ref desc, IntPtr.Zero, out var stagingPtr);
        return stagingPtr;
    }

    public static void CopyResource(IntPtr contextPtr, IntPtr dst, IntPtr src)
    {
        var vtable = Marshal.ReadIntPtr(contextPtr);
        var fn = Marshal.ReadIntPtr(vtable, 47 * IntPtr.Size);
        var del = Marshal.GetDelegateForFunctionPointer<CopyResourceDelegate>(fn);
        del(contextPtr, dst, src);
    }

    public static byte[]? ReadTextureBytes(IntPtr contextPtr, IntPtr stagingPtr, int width, int height)
    {
        var vtable = Marshal.ReadIntPtr(contextPtr);

        var mapFn = Marshal.ReadIntPtr(vtable, 14 * IntPtr.Size);
        var mapDel = Marshal.GetDelegateForFunctionPointer<MapDelegate>(mapFn);
        mapDel(contextPtr, stagingPtr, 0, 1, 0, out var mapped);

        if (mapped.pData == IntPtr.Zero)
        {
            Console.WriteLine("Map failed => pData = NULL");
            return null;
        }

        var bytes = new byte[width * height * 4];

        for (int y = 0; y < height; y++)
        {
            IntPtr src = IntPtr.Add(mapped.pData, (int)(y * mapped.RowPitch));
            Marshal.Copy(src, bytes, y * width * 4, width * 4);
        }

        var unmapFn = Marshal.ReadIntPtr(vtable, 15 * IntPtr.Size);
        var unmapDel = Marshal.GetDelegateForFunctionPointer<UnmapDelegate>(unmapFn);
        unmapDel(contextPtr, stagingPtr, 0);

        return bytes;
    }

    // ── Structs ────────────────────────────────────────────────────────────────

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DXGI_OUTPUT_DESC
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
        public int DesktopCoordinatesLeft, DesktopCoordinatesTop;
        public int DesktopCoordinatesRight, DesktopCoordinatesBottom;
        public bool AttachedToDesktop;
        public uint Rotation;
        public IntPtr Monitor;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DXGI_OUTDUPL_FRAME_INFO
    {
        public long LastPresentTime, LastMouseUpdateTime;
        public uint AccumulatedFrames;
        public bool RectsCoalesced, ProtectedContentMaskedOut;
        public DXGI_OUTDUPL_POINTER_POSITION PointerPosition;
        public uint TotalMetadataBufferSize, PointerShapeBufferSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DXGI_OUTDUPL_POINTER_POSITION
    {
        public int X, Y;
        public bool Visible;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct D3D11_TEXTURE2D_DESC
    {
        public uint Width, Height, MipLevels, ArraySize, Format;
        public uint SampleDescCount, SampleDescQuality;
        public uint Usage, BindFlags, CPUAccessFlags, MiscFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct D3D11_MAPPED_SUBRESOURCE
    {
        public IntPtr pData;
        public uint RowPitch, DepthPitch;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DXGI_ADAPTER_DESC
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string Description;
        public uint VendorId;
        public uint DeviceId;
        public uint SubSysId;
        public uint Revision;
        public IntPtr DedicatedVideoMemory;
        public IntPtr DedicatedSystemMemory;
        public IntPtr SharedSystemMemory;
        public long AdapterLuid;
    }

    private delegate void GetDescDelegate(IntPtr self, [Out] int[] desc);
    private delegate int CreateTexture2DDelegate(IntPtr self, ref D3D11_TEXTURE2D_DESC desc, IntPtr init, out IntPtr tex);
    private delegate void CopyResourceDelegate(IntPtr self, IntPtr dst, IntPtr src);
    private delegate int MapDelegate(IntPtr self, IntPtr resource, uint sub, uint mapType, uint flags, out D3D11_MAPPED_SUBRESOURCE mapped);
    private delegate void UnmapDelegate(IntPtr self, IntPtr resource, uint sub);
}