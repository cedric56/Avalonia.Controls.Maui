using Microsoft.Maui.Devices.Sensors;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using System.Text.Json.Serialization;
using MauiLocation = Microsoft.Maui.Devices.Sensors.Location;

namespace Avalonia.Controls.Maui.Essentials;

// DTO used to deserialize browser geolocation JSON result
internal class GeolocationReadingResultInterop
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("altitude")]
    public double? Altitude { get; set; }

    [JsonPropertyName("accuracy")]
    public double Accuracy { get; set; }

    [JsonPropertyName("altitudeAccuracy")]
    public double? AltitudeAccuracy { get; set; }

    [JsonPropertyName("speed")]
    public double? Speed { get; set; }

    [JsonPropertyName("heading")]
    public double? Course { get; set; }
}

// Source-generated JSON serializer context (avoids reflection, better perf for WASM)
[JsonSerializable(typeof(GeolocationReadingResultInterop))]
internal partial class AvaloniaJsonSerializerContext : JsonSerializerContext
{
}

partial class AvaloniaGeolocation
{
    const string ModuleName = "geolocation";
    public AvaloniaGeolocation()
    {
        // Load JS module containing geolocation interop functions
        _ = JSHost.ImportAsync(ModuleName, "/geolocation.browser.js");
    }

    // ID returned by browser's watchPosition (used to stop listening)
    int watchingId = -1;

    // Checks if browser supports geolocation API
    [JSImport("geolocationInterop.isSupported", ModuleName)]
    internal static partial bool IsSupported();

    // One-shot location request (navigator.geolocation.getCurrentPosition)
    [JSImport("geolocationInterop.getCurrentLocation", ModuleName)]
    internal static partial Task<string> GetCurrentLocation(        
        bool highAccuracy,
        [JSMarshalAs<JSType.Number>] double timeout);

    // Starts continuous location updates (navigator.geolocation.watchPosition)
    [JSImport("geolocationInterop.startLocationReading", ModuleName)]
    internal static partial int StartLocationReadingInterop(
        [JSMarshalAs<JSType.Function<JSType.String>>] Action<string> onSuccess,
        [JSMarshalAs<JSType.Function<JSType.Number, JSType.String>>] Action<short, string> onError,
        bool highAccuracy = false);

    // Stops continuous updates
    [JSImport("geolocationInterop.stopLocationReading", ModuleName)]
    internal static partial void StopLocationReadingInterop([JSMarshalAs<JSType.Number>] int id);

    // Indicates if we are currently listening (watchPosition active)
    public bool PlatformIsListeningForeground() => watchingId != -1;

    // Browser capability check
    public bool PlatformIsEnabled() => IsSupported();

    public async Task<MauiLocation?> PlatformGetLocationAsync(GeolocationRequest request, CancellationToken cancelToken)
    {
        // NOTE: Browser API does not support CancellationToken directly
        // Timeout should be handled in JS if needed

        var json = await GetCurrentLocation(
            request.RequestFullAccuracy || request.DesiredAccuracy is GeolocationAccuracy.High or GeolocationAccuracy.Best,
            request.Timeout == TimeSpan.Zero ? double.PositiveInfinity : request.Timeout.TotalMilliseconds)
            .WaitAsync(cancelToken);

        var result = JsonSerializer.Deserialize(
            json,
            AvaloniaJsonSerializerContext.Default.GeolocationReadingResultInterop);

        if (result is null)
            return null;

        // Map browser result → MAUI Location
        return _lastKnownLocation = new MauiLocation(result.Latitude, result.Longitude)
        {
            Altitude = result.Altitude,
            Accuracy = result.Accuracy,
            Speed = result.Speed,
            Course = result.Course
        };
    }

    public Task<bool> PlatformStartListeningForegroundAsync(GeolocationListeningRequest request)
    {
        // Browser only supports a boolean "enableHighAccuracy"
        // So we map MAUI accuracy to a simple high/low flag
        watchingId = StartLocationReadingInterop(
            json => OnSuccessInterop(json, request.MinimumTime),
            OnErrorInterop,
            request.DesiredAccuracy is GeolocationAccuracy.High or GeolocationAccuracy.Best);

        return Task.FromResult(watchingId != -1);
    }

    public void PlatformStopListeningForeground()
    {
        // Stop browser watchPosition if active
        if (watchingId != -1)
        {
            StopLocationReadingInterop(watchingId);
            watchingId = -1;
        }
    }

    void OnSuccessInterop(string jsonData, TimeSpan minimumTime)
    {
        // Called by JS when a new position is available
        var result = JsonSerializer.Deserialize(
            jsonData,
            AvaloniaJsonSerializerContext.Default.GeolocationReadingResultInterop);

        if (result is null)
            return;

        // Throttle updates according to MinimumTime (MAUI behavior)
        if (DateTimeOffset.UtcNow - _lastUpdateTime < minimumTime)
            return;

        // Update throttling timestamp
        _lastUpdateTime = DateTimeOffset.UtcNow;

        // Convert to MAUI Location and raise event
        OnLocationChanged(_lastKnownLocation = new MauiLocation(result.Latitude, result.Longitude)
        {
            Altitude = result.Altitude,
            Accuracy = result.Accuracy,
            Speed = result.Speed,
            Course = result.Course
        });
    }

    void OnErrorInterop(short code, string message)
    {
        // Browser error codes:
        // 1 = PERMISSION_DENIED
        // 2 = POSITION_UNAVAILABLE
        // 3 = TIMEOUT

        if (code == 1)
        {
            OnLocationError(GeolocationError.Unauthorized);
        }
        else
        {
            // MAUI has no Timeout distinction → map to PositionUnavailable
            OnLocationError(GeolocationError.PositionUnavailable);
        }
    }
}