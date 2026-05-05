using Microsoft.Maui.Devices.Sensors;

namespace ControlGallery.Pages;

public partial class GeolocationPage : ContentPage
{
    public GeolocationPage()
    {
        InitializeComponent();
    }

    private async void OnGetLocationClicked(object sender, EventArgs e)
    {
        try
        {
            var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10));
            var location = await Geolocation.Default.GetLocationAsync(request);

            UpdateLocation(location);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void OnGetLastKnownClicked(object sender, EventArgs e)
    {
        try
        {
            var location = await Geolocation.Default.GetLastKnownLocationAsync();
            UpdateLocation(location);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void OnStartListeningClicked(object sender, EventArgs e)
    {
        try
        {
            var request = new GeolocationListeningRequest(
                GeolocationAccuracy.Best,
                TimeSpan.FromSeconds(2));

            Geolocation.Default.LocationChanged += OnLocationChanged;
            Geolocation.Default.ListeningFailed += OnListeningFailed;

            await Geolocation.Default.StartListeningForegroundAsync(request);

            ListeningStatusLabel.Text = "Status: Listening...";
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private void OnStopListeningClicked(object sender, EventArgs e)
    {
        Geolocation.Default.StopListeningForeground();

        Geolocation.Default.LocationChanged -= OnLocationChanged;
        Geolocation.Default.ListeningFailed -= OnListeningFailed;

        ListeningStatusLabel.Text = "Status: Stopped";
    }

    private void OnLocationChanged(object? sender, GeolocationLocationChangedEventArgs e)
    {
        Dispatcher.Dispatch(() =>
        {
            UpdateLocation(e.Location);
        });
    }

    private void OnListeningFailed(object? sender, GeolocationListeningFailedEventArgs e)
    {
        Dispatcher.Dispatch(async () =>
        {
            await DisplayAlert("Listening failed", e.Error.ToString(), "OK");
        });
    }

    private void UpdateLocation(Location? location)
    {
        if (location == null)
        {
            LatitudeLabel.Text = "Latitude: --";
            LongitudeLabel.Text = "Longitude: --";
            AccuracyLabel.Text = "Accuracy: --";
            return;
        }

        LatitudeLabel.Text = $"Latitude: {location.Latitude}";
        LongitudeLabel.Text = $"Longitude: {location.Longitude}";
        AccuracyLabel.Text = $"Accuracy: {location.Accuracy}";
    }
}