using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using System.Runtime.Versioning;
using Windows.Devices.Sensors;
using Sensor = Windows.Devices.Sensors.Barometer;

namespace Avalonia.Controls.Maui.Essentials
{
    /// <summary>
    /// Base platform-agnostic implementation for barometer (atmospheric pressure sensor).
    /// Provides default behavior for unsupported platforms - throws feature not supported exceptions.
    /// Platform-specific implementations override the virtual methods to provide actual sensor functionality.
    /// The barometer measures atmospheric pressure in hectopascals (hPa), also known as millibars.
    /// </summary>
    partial class AvaloniaBarometer
    {
        /// <summary>
        /// Indicates whether the barometer is supported on the current platform.
        /// Base implementation returns false, indicating no support.
        /// Platform-specific implementations override this to return actual support status.
        /// </summary>
        public override bool IsSupported => false;

        /// <summary>
        /// Platform-specific implementation to start barometer monitoring.
        /// Base implementation throws FeatureNotSupportedException.
        /// </summary>
        /// <param name="sensorSpeed">Desired sensor update speed/precision</param>
        protected override void PlatformStart(SensorSpeed sensorSpeed)
        {
            throw new FeatureNotSupportedException();
        }

        /// <summary>
        /// Platform-specific implementation to stop barometer monitoring.
        /// Base implementation throws FeatureNotSupportedException.
        /// </summary>
        protected override void PlatformStop()
        {
            throw new FeatureNotSupportedException();
        }
    }

    /// <summary>
    /// Windows-specific implementation of barometer using the UWP Windows.Devices.Sensors.Barometer API.
    /// Supported on Windows 10 build 10240 and later.
    /// Measures atmospheric pressure in hectopascals (hPa).
    /// Uses station pressure (actual local pressure) rather than sea-level adjusted pressure.
    /// </summary>
    [SupportedOSPlatform("windows10.0.10240")]
    class WindowsBarometer : AvaloniaBarometer
    {
        Sensor? sensor;  // Reference to the Windows barometer sensor

        /// <summary>
        /// Gets the default barometer sensor on the Windows device.
        /// Returns null if no barometer is present (e.g., devices without pressure sensor).
        /// </summary>
        Sensor DefaultBarometer => Sensor.GetDefault();

        /// <summary>
        /// Indicates whether a barometer is available on this Windows device.
        /// Returns true if the default sensor exists, false otherwise.
        /// </summary>
        public override bool IsSupported =>
            DefaultBarometer != null;

        /// <summary>
        /// Starts the Windows barometer with the specified sensor speed.
        /// Configures the report interval based on the requested speed and minimum sensor capabilities.
        /// </summary>
        /// <param name="sensorSpeed">Desired sensor update frequency/precision</param>
        protected override void PlatformStart(SensorSpeed sensorSpeed)
        {
            // Convert SensorSpeed enum to interval in milliseconds
            var interval = sensorSpeed.ToPlatform();

            // Get the default barometer sensor
            sensor = DefaultBarometer;

            // Set the report interval - use the larger of requested interval or sensor's minimum
            // This respects both the app's desired update rate and the sensor's hardware limitations
            sensor.ReportInterval = sensor.MinimumReportInterval >= interval ? sensor.MinimumReportInterval : interval;

            // Subscribe to reading changed events
            sensor.ReadingChanged += BarometerReportedInterval;
        }

        /// <summary>
        /// Stops the Windows barometer.
        /// Unsubscribes from events and resets the report interval to default.
        /// </summary>
        protected override void PlatformStop()
        {
            if (sensor == null)
                return;

            // Unsubscribe from reading events
            sensor.ReadingChanged -= BarometerReportedInterval;

            // Reset report interval to 0 (default, no minimum interval)
            sensor.ReportInterval = 0;

            // Release the sensor reference
            sensor = null;
        }

        /// <summary>
        /// Event handler for Windows barometer readings.
        /// Extracts the station pressure in hectopascals and raises the ReadingChanged event.
        /// Station pressure is the actual atmospheric pressure at the device's location,
        /// not adjusted to sea level. This is useful for altitude calculations and local weather.
        /// </summary>
        /// <param name="sender">The barometer sensor object</param>
        /// <param name="e">Event args containing the barometer reading</param>
        internal void BarometerReportedInterval(object sender, BarometerReadingChangedEventArgs e)
            => RaiseReadingChanged(new BarometerData(e.Reading.StationPressureInHectopascals));
    }

    /// <summary>
    /// Linux-specific implementation of barometer using the Linux Industrial I/O (IIO) subsystem.
    /// Reads atmospheric pressure data directly from sysfs files in /sys/bus/iio/devices/.
    /// Uses polling since Linux doesn't provide event-based sensor APIs by default.
    /// Pressure is typically reported in kilopascals (kPa) or hectopascals (hPa) depending on the sensor.
    /// </summary>
    class LinuxBarometer : AvaloniaBarometer
    {
        private CancellationTokenSource? _cts;      // Token source for cancelling the polling loop
        private Task? _pollingTask;                 // Background task for polling sensor data
        private readonly string? _devicePath;       // Path to the IIO device directory (e.g., /sys/bus/iio/devices/iio:device0)

        /// <summary>
        /// Constructor - attempts to locate a barometer/pressure sensor in the Linux IIO subsystem.
        /// Scans /sys/bus/iio/devices/ for a device that has pressure raw data files.
        /// </summary>
        public LinuxBarometer()
        {
            if (Directory.Exists("/sys/bus/iio/devices/"))
            {
                // Search through all IIO devices to find one with barometer/pressure capability
                foreach (var dir in Directory.GetDirectories("/sys/bus/iio/devices/"))
                {
                    // Check for the presence of pressure raw data file
                    if (File.Exists(Path.Combine(dir, "in_pressure_raw")))
                    {
                        _devicePath = dir;
                        break;  // Stop searching once we find the first pressure sensor
                    }
                }
            }
        }

        /// <summary>
        /// Indicates whether a barometer was found in the Linux IIO subsystem.
        /// Returns true if a device with pressure sensor files was discovered.
        /// </summary>
        public override bool IsSupported => _devicePath is not null;

        /// <summary>
        /// Starts polling the Linux barometer.
        /// Launches a background task that continuously reads pressure data from sysfs files.
        /// </summary>
        /// <param name="sensorSpeed">Desired sensor update frequency (used to determine polling interval)</param>
        protected override void PlatformStart(SensorSpeed sensorSpeed)
        {
            if (_devicePath == null)
                throw new NotSupportedException("No barometer found in /sys/bus/iio/devices");

            // Create cancellation token source for stopping the polling loop
            _cts = new CancellationTokenSource();

            // Start the background polling task
            _pollingTask = Task.Run(() => PollingLoop(sensorSpeed, _cts.Token));
        }

        /// <summary>
        /// Stops polling the Linux barometer.
        /// Cancels the background task and waits for it to complete.
        /// </summary>
        async protected override void PlatformStop()
        {
            // Signal the polling loop to stop
            _cts?.Cancel();

            if (_pollingTask != null)
            {
                try
                {
                    // Wait for the polling task to complete gracefully
                    await _pollingTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected exception when cancellation is requested - swallow it
                }
            }
        }

        /// <summary>
        /// Main polling loop that continuously reads barometer/pressure data from sysfs files.
        /// Runs on a background thread until cancellation is requested.
        /// </summary>
        /// <param name="sensorSpeed">Sensor speed (used to determine polling delay)</param>
        /// <param name="token">Cancellation token to stop the polling loop</param>
        private void PollingLoop(SensorSpeed sensorSpeed, CancellationToken token)
        {
            if (_devicePath == null)
                throw new NotSupportedException("No barometer found in /sys/bus/iio/devices");

            // Read scale factor (converts raw ADC values to physical units)
            double scale = 1.0;
            var scalePath = Path.Combine(_devicePath, "in_pressure_scale");
            if (File.Exists(scalePath))
                scale = double.Parse(File.ReadAllText(scalePath).Trim(),
                    System.Globalization.CultureInfo.InvariantCulture);  // Use invariant culture for decimal parsing

            // Continue polling until cancellation is requested
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Read raw pressure value and apply scale factor to get physical reading
                    double pressure = ReadRaw("in_pressure_raw") * scale;

                    // Create MAUI barometer data and raise the ReadingChanged event
                    var data = new BarometerData(pressure);
                    RaiseReadingChanged(data);
                }
                catch (Exception ex)
                {
                    // Log errors but continue polling to maintain sensor functionality
                    Console.Error.WriteLine($"Error reading barometer : {ex.Message}");
                }

                // Wait for the specified interval before the next reading
                // Typical pressure sensors support 10-50 Hz, adjust based on sensorSpeed
                Thread.Sleep((int)sensorSpeed.ToPlatform());
            }
        }

        /// <summary>
        /// Reads a raw value from a sysfs file in the IIO device directory.
        /// Handles file reading and parsing with invariant culture for consistent decimal parsing.
        /// </summary>
        /// <param name="filename">The sysfs filename to read (e.g., "in_pressure_raw")</param>
        /// <returns>The raw integer or floating-point value read from the file</returns>
        private double ReadRaw(string filename)
        {
            if (_devicePath == null)
                throw new NotSupportedException("No barometer found in /sys/bus/iio/devices");

            var path = Path.Combine(_devicePath, filename);
            if (!File.Exists(path)) return 0;

            // Read and parse the value using invariant culture to handle decimal separators correctly
            // (Linux uses '.' while some cultures use ',' as decimal separator)
            var text = File.ReadAllText(path).Trim();
            return double.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}