using Android.Views;
using Microsoft.Maui.Handlers;

namespace VirtualScreenViewer.Platforms.Android;

public class VideoSurfaceViewHandler : ViewHandler<VideoSurfaceView, SurfaceView>
{
    public static readonly IPropertyMapper<VideoSurfaceView, VideoSurfaceViewHandler> Mapper = new PropertyMapper<VideoSurfaceView, VideoSurfaceViewHandler>();

    public VideoSurfaceViewHandler() : base(Mapper) { }

    protected override SurfaceView CreatePlatformView()
        => new SurfaceView(Context);

    protected override void ConnectHandler(SurfaceView platformView)
    {
        base.ConnectHandler(platformView);
        // Powiadom VideoSurfaceView że SurfaceView jest gotowy
    }
}