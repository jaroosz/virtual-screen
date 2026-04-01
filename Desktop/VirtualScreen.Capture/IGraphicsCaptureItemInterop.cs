using System.Runtime.InteropServices;
using Windows.Graphics.Capture;

namespace VirtualScreen.Capture;

[Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[ComImport]
internal interface IGraphicsCaptureItemInterop
{
    GraphicsCaptureItem CreateForMonitor(
        IntPtr hMonitor,
        [In] ref Guid iid);
}