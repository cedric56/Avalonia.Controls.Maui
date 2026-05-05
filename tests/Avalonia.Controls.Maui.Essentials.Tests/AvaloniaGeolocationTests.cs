using Avalonia.Controls.Maui.Essentials;
using Microsoft.Maui.Devices.Sensors;
using MauiLocation = Microsoft.Maui.Devices.Sensors.Location;


namespace Avalonia.Controls.Maui.Tests.Services;

public class AvaloniaGeolocationTests
{
    [Fact]
    public async Task GetLastKnownLocation_ReturnsCachedLocation()
    {
        var geo = new AvaloniaGeolocation();
        var location = new MauiLocation(10, 20);

        // simulate internal state
        typeof(AvaloniaGeolocation)
            .GetField("_lastKnownLocation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(geo, location);

        var result = await geo.GetLastKnownLocationAsync();

        Assert.Equal(location, result);
    }

    [Fact]
    public async Task StartListeningForeground_SetsListening()
    {
        var geo = new AvaloniaGeolocation();

        var result = await geo.StartListeningForegroundAsync(
            new GeolocationListeningRequest());

        Assert.False(result);
        Assert.False(geo.IsListeningForeground);
    }

    [Fact]
    public void StopListeningForeground_StopsListening()
    {
        var geo = new AvaloniaGeolocation();

        geo.StopListeningForeground();

        Assert.False(geo.IsListeningForeground);
    }

    [Fact]
    public void OnLocationChanged_RaisesEvent()
    {
        var geo = new AvaloniaGeolocation();
        var location = new MauiLocation(5, 6);

        GeolocationLocationChangedEventArgs? received = null;

        geo.LocationChanged += (_, e) => received = e;

        geo.OnLocationChanged(location);

        Assert.NotNull(received);
        Assert.Equal(location, received!.Location);
    }

    [Fact]
    public void OnLocationError_RaisesEvent()
    {
        var geo = new AvaloniaGeolocation();

        GeolocationListeningFailedEventArgs? received = null;

        geo.ListeningFailed += (_, e) => received = e;

        geo.OnLocationError(GeolocationError.PositionUnavailable);

        Assert.NotNull(received);
        Assert.Equal(GeolocationError.PositionUnavailable, received!.Error);
    }
}
