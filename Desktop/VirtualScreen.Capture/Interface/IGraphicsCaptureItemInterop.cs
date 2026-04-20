using System.Runtime.InteropServices;
using Windows.Graphics.Capture;

namespace VirtualScreen.Capture.Interface;

[ComImport]
[Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IGraphicsCaptureItemInterop
{
    int CreateForWindow(IntPtr hwnd, ref Guid riid, out IntPtr ppvObject);
    int CreateForMonitor(IntPtr hMonitor, ref Guid riid, out IntPtr ppvObject);
}