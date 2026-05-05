using Microsoft.Maui.Devices.Sensors;
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;
using MauiLocation = Microsoft.Maui.Devices.Sensors.Location;

namespace Avalonia.Controls.Maui.Essentials;

partial class AvaloniaGeolocation
{
    // Persistent DBus connection required for listening mode
    private Connection? _connection;

    // GeoClue client proxy (represents a location session)
    private OrgFreedesktopGeoClue2ClientProxy? _client;

    // Subscription to DBus signals (location updates)
    private IDisposable? _locationChangedSubscription;

    // GeoClue DBus constants
    const string GeoClueService = "org.freedesktop.GeoClue2";
    const string GeoCluePath = "/org/freedesktop/GeoClue2/Manager";
    //const string Address = "unix:path=/var/run/dbus/system_bus_socket";

    // Indicates if we are actively listening (subscription exists)
    public bool PlatformIsListeningForeground() => _locationChangedSubscription != null;

    // GeoClue is always "enabled" from MAUI perspective (no OS toggle exposed here)
    public bool PlatformIsEnabled() => true;

    public async Task<MauiLocation?> PlatformGetLocationAsync(GeolocationRequest request, CancellationToken cancelToken)
    {
        if (OperatingSystem.IsLinux())
        {
            // Combine external cancellation with timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);

            // Apply MAUI timeout manually (GeoClue has no built-in timeout)
            if (request.Timeout > TimeSpan.Zero)
                cts.CancelAfter(request.Timeout);

            // Create a short-lived DBus connection (one-shot request)
            using var connection = new Connection(Address.Session!);
            await connection.ConnectAsync();
            
            // Create and start GeoClue client
            var client = await GetClientProxy(connection, cts.Token);

            // Ensure client is active before requesting location
            if (await client.GetActivePropertyAsync())
            {
                // Apply requested accuracy (mapped to GeoClue levels)
                await client.SetRequestedAccuracyLevelPropertyAsync(
                    MapAccuracy(request.DesiredAccuracy, request.RequestFullAccuracy));

                // Retrieve current location object path
                var locationPath = await client.GetLocationPropertyAsync();

                // Fetch actual location data
                var location = await UpdateLocation(connection, locationPath);

                return location;
            }
        }
        return null;
    }

    public async Task<bool> PlatformStartListeningForegroundAsync(GeolocationListeningRequest request)
    {
        if (!OperatingSystem.IsLinux())
            return false;

        try
        {
            // Persistent connection required to receive DBus signals
            _connection = new Connection(Address.Session!);
            await _connection.ConnectAsync();

            // Create GeoClue client
            _client = await GetClientProxy(_connection, CancellationToken.None);

            if (await _client.GetActivePropertyAsync())
            {
                // Apply requested accuracy
                await _client.SetRequestedAccuracyLevelPropertyAsync(
                    MapAccuracy(request.DesiredAccuracy));

                // Subscribe to DBus property changes (GeoClue pushes updates this way)
                _locationChangedSubscription = await _client.WatchLocationUpdatedAsync(
                    async (exception, changes) =>
                    {
                        // Throttle updates according to MinimumTime (MAUI behavior)
                        if (DateTimeOffset.UtcNow - _lastUpdateTime < request.MinimumTime)
                            return;

                        try
                        {
                            // Fetch updated location data
                            var location = await UpdateLocation(_connection, changes.New);

                            // Raise MAUI event
                            OnLocationChanged(location);
                        }
                        catch
                        {
                            // Any failure while retrieving location → notify error
                            OnLocationError(GeolocationError.PositionUnavailable);
                        }
                    });

                return true;
            }
            else
            {
                // GeoClue client failed to activate
                OnLocationError(GeolocationError.Unauthorized);
                return false;
            }
        }
        catch
        {
            // Typically DBus permission or access issue
            OnLocationError(GeolocationError.Unauthorized);
            return false;
        }
    }

    // Maps MAUI accuracy to GeoClue accuracy levels (1–6)
    private static uint MapAccuracy(GeolocationAccuracy accuracy, bool requestFullAccuracy = false)
    {
        // MAUI "RequestFullAccuracy" forces highest level
        if (requestFullAccuracy)
            return 6; // Exact

        return accuracy switch
        {
            GeolocationAccuracy.Lowest => 2,   // Country
            GeolocationAccuracy.Low => 3,      // City
            GeolocationAccuracy.Medium => 4,   // Neighborhood
            GeolocationAccuracy.High => 5,     // Street
            GeolocationAccuracy.Best => 6,     // Exact
            _ => 4
        };
    }

    public void PlatformStopListeningForeground()
    {
        // Stop receiving DBus signals
        _locationChangedSubscription?.Dispose();
        _locationChangedSubscription = null;

        // Release client and connection
        _client = null;
        _connection?.Dispose();
        _connection = null;
    }

    private async Task<OrgFreedesktopGeoClue2ClientProxy> GetClientProxy(Connection connection, CancellationToken token)
    {
        // GeoClue manager creates a new client session
        var manager = new OrgFreedesktopGeoClue2ManagerProxy(
            connection, GeoClueService, GeoCluePath);

        var clientPath = await manager.CreateClientAsync().WaitAsync(token);

        //Any agent need to be set ??
        //await manager.AddAgentAsync("essentials").WaitAsync(token);        
        //var agent = new OrgFreedesktopGeoClue2AgentProxy(connection,
        //        GeoClueService,
        //        )

        var client = new OrgFreedesktopGeoClue2ClientProxy(
            connection,
            GeoClueService,
            clientPath.ToString());

        // Required for GeoClue identification (desktop app id)
        await client.SetDesktopIdPropertyAsync("essentials");  

        // Start location updates
        await client.StartAsync();

        return client;
    }

    private async Task<MauiLocation> UpdateLocation(Connection connection, ObjectPath newPath)
    {
        // Create proxy for the location object
        var proxy = new OrgFreedesktopGeoClue2LocationProxy(
            connection,
            GeoClueService,
            newPath.ToString());

        // Retrieve all available location data
        var latitude = await proxy.GetLatitudePropertyAsync();
        var longitude = await proxy.GetLongitudePropertyAsync();
        var accuracy = await proxy.GetAccuracyPropertyAsync();
        var altitude = await proxy.GetAltitudePropertyAsync();
        var speed = await proxy.GetSpeedPropertyAsync();

        // GeoClue timestamp is (seconds, microseconds)
        var timeStamp = await proxy.GetTimestampPropertyAsync();

        // Build MAUI Location object
        var location = new MauiLocation(latitude, longitude, altitude)
        {
            Accuracy = accuracy,
            Speed = speed,
            Timestamp = FromUnixTuple(timeStamp.Item1, timeStamp.Item2)
        };

        // Update throttling timestamp
        _lastUpdateTime = DateTimeOffset.UtcNow;

        // Cache last known location
        return _lastKnownLocation = location;
    }

    // Converts GeoClue timestamp (seconds, microseconds) → DateTimeOffset
    public static DateTimeOffset FromUnixTuple(ulong seconds, ulong microseconds)
    {
        var dto = DateTimeOffset.FromUnixTimeSeconds((long)seconds);

        // 1 microsecond = 10 ticks (100 ns per tick)
        return dto.AddTicks((long)microseconds * 10);
    }
}