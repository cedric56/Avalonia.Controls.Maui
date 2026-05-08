using Microsoft.Maui.Devices.Sensors;
using MauiLocation = Microsoft.Maui.Devices.Sensors.Location;

namespace Avalonia.Controls.Maui.Essentials;

partial class AvaloniaGeolocation : IGeolocation
{
    MauiLocation? _lastKnownLocation;

    // Used to throttle updates based on MinimumTime
    DateTimeOffset _lastUpdateTime = DateTimeOffset.MinValue;

    public bool IsListeningForeground => PlatformIsListeningForeground();

    public bool IsEnabled => PlatformIsEnabled();

    public event EventHandler<GeolocationLocationChangedEventArgs>? LocationChanged;
    public event EventHandler<GeolocationListeningFailedEventArgs>? ListeningFailed;

    public Task<MauiLocation?> GetLastKnownLocationAsync()
    {
        return Task.FromResult(_lastKnownLocation);
    }

    public Task<MauiLocation?> GetLocationAsync(GeolocationRequest request, CancellationToken cancelToken)
    {
        return PlatformGetLocationAsync(request, cancelToken);
    }

    public Task<bool> StartListeningForegroundAsync(GeolocationListeningRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.MinimumTime.TotalMilliseconds < 0)
            throw new ArgumentOutOfRangeException(nameof(request), "MinimumTime must be positive.");

        if (IsListeningForeground)
            throw new InvalidOperationException("Already listening to location changes.");


        return PlatformStartListeningForegroundAsync(request);
    }

    public void StopListeningForeground()
    {
        if (!IsListeningForeground)
            return;

        PlatformStopListeningForeground();
    }

    internal void OnLocationChanged(MauiLocation location) =>
            OnLocationChanged(new GeolocationLocationChangedEventArgs(location));

    internal void OnLocationChanged(GeolocationLocationChangedEventArgs e) =>
        LocationChanged?.Invoke(null, e);

    internal void OnLocationError(GeolocationError geolocationError)
    {
        PlatformStopListeningForeground();

        ListeningFailed?.Invoke(null, new GeolocationListeningFailedEventArgs(geolocationError));
    }
}
