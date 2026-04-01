using System.Net;
using VirtualScreenViewer.Services;
using static VirtualScreenViewer.Services.StreamReceiver;

namespace VirtualScreenViewer;

public partial class MainPage : ContentPage
{
    private readonly StreamReceiver _receiver;
    private const int StreamPort = 5555;

    public MainPage()
    {
        InitializeComponent();

        _receiver = new StreamReceiver();
        _receiver.FrameReceived += OnFrameReceived;
        _receiver.ConnectionStatusChanged += OnConnectionStatusChanged;
    }

    private async void OnConnectionClicked(object sender, EventArgs e)
    {
        if (_receiver.IsConnected)
        {
            _receiver.Disconnect();
            ConnectButton.Text = "Connect";
            return;
        }

        var ipAddress = IpAddressEntry.Text?.Trim();
        if (string.IsNullOrEmpty(ipAddress))
        {
            await DisplayAlert("Error", "Please enter desktop IP address", "OK");
            return;
        }

        try
        {
            ConnectButton.IsEnabled = false;
            await _receiver.ConnectAsync(ipAddress, StreamPort);
            ConnectButton.Text = "Disconnect";
        }
        catch (Exception ex)
        {
            await DisplayAlert("Connection Error", ex.Message, "OK");
        }
        finally
        {
            ConnectButton.IsEnabled = true;
        }
    }

    private void OnFrameReceived(object? sender, FrameReceivedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                var imageSource = ImageSource.FromStream(() => new MemoryStream(e.ImageData));
                VideoImage.Source = imageSource;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Frame decode error: {ex.Message}");
            }
        });
    }

    private void OnConnectionStatusChanged(object? sender, string status)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            StatusLabel.Text = status;
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _receiver.Dispose();
    }
}
