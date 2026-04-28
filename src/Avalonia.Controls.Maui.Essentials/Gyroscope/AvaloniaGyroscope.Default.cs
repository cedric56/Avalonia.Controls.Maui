using Microsoft.Maui.Devices.Sensors;
using System.Runtime.Versioning;
using Windows.Devices.Sensors;
using Sensor = Windows.Devices.Sensors.Gyrometer;

namespace Avalonia.Controls.Maui.Essentials
{
    /// <summary>
    /// Base platform-agnostic implementation for gyroscope (angular velocity sensor).
    /// Provides default behavior for unsupported platforms - throws not supported exceptions.
    /// Platform-specific implementations override the virtual methods to provide actual sensor functionality.
    /// The gyroscope measures angular velocity (rotation rate) in radians per second (rad/s)
    /// around the device's three axes (X: pitch, Y: roll, Z: yaw).
    /// </summary>
    partial class AvaloniaGyroscope
    {
        /// <summary>
        /// Indicates whether the gyroscope is supported on the current platform.
        /// Base implementation returns false, indicating no support.
        /// Platform-specific implementations override this to return actual support status.
        /// </summary>
        public override bool IsSupported => false;

        /// <summary>
        /// Platform-specific implementation to start gyroscope monitoring.
        /// Base implementation throws NotSupportedException.
        /// </summary>
        /// <param name="sensorSpeed">Desired sensor update speed/precision</param>
        protected override void PlatformStart(SensorSpeed sensorSpeed) =>
            throw new NotSupportedException();

        /// <summary>
        /// Platform-specific implementation to stop gyroscope monitoring.
        /// Base implementation throws NotSupportedException.
        /// </summary>
        protected override void PlatformStop() =>
            throw new NotSupportedException();
    }

    /// <summary>
    /// Windows-specific implementation of gyroscope using the UWP Windows.Devices.Sensors.Gyrometer API.
    /// Supported on Windows 10 build 10240 and later.
    /// Measures angular velocity in radians per second (rad/s) around X, Y, and Z axes.
    /// </summary>
    [SupportedOSPlatform("windows10.0.10240")]
    class WindowsGyroscope : AvaloniaGyroscope
    {
        // Keep a reference to the sensor so we can clean up and stop the same instance
        Sensor? sensor;

        /// <summary>
        /// Gets the default gyrometer sensor on the Windows device.
        /// Returns null if no gyroscope is present.
        /// </summary>
        static Sensor DefaultSensor =>
            Sensor.GetDefault();

        /// <summary>
        /// Indicates whether a gyroscope is available on this Windows device.
        /// Returns true if the default sensor exists, false otherwise.
        /// </summary>
        public override bool IsSupported => DefaultSensor != null;

        /// <summary>
        /// Starts the Windows gyroscope with the specified sensor speed.
        /// Configures the report interval based on the requested speed and minimum sensor capabilities.
        /// </summary>
        /// <param name="sensorSpeed">Desired sensor update frequency/precision</param>
        protected override void PlatformStart(SensorSpeed sensorSpeed)
        {
            // Get the default gyroscope sensor
            sensor = DefaultSensor;

            // Convert SensorSpeed enum to interval in milliseconds
            var interval = sensorSpeed.ToPlatform();

            // Set the report interval - use the larger of requested interval or sensor's minimum
            // This respects both the app's desired update rate and the sensor's hardware limitations
            sensor.ReportInterval = sensor.MinimumReportInterval >= interval ? sensor.MinimumReportInterval : interval;

            // Subscribe to reading changed events
            sensor.ReadingChanged += DataUpdated;
        }

        /// <summary>
        /// Event handler for Windows gyroscope readings.
        /// Extracts angular velocity data in radians per second and raises the ReadingChanged event.
        /// </summary>
        /// <param name="sender">The gyrometer sensor object</param>
        /// <param name="e">Event args containing the gyroscope reading</param>
        void DataUpdated(object sender, GyrometerReadingChangedEventArgs e)
        {
            var reading = e.Reading;
            // AngularVelocity values are in radians per second (rad/s)
            var data = new GyroscopeData(reading.AngularVelocityX, reading.AngularVelocityY, reading.AngularVelocityZ);
            RaiseReadingChanged(data);
        }

        /// <summary>
        /// Stops the Windows gyroscope.
        /// Unsubscribes from events and resets the report interval to default.
        /// </summary>
        protected override void PlatformStop()
        {
            if (sensor == null)
                return;

            // Unsubscribe from reading events
            sensor.ReadingChanged -= DataUpdated;

            // Reset report interval to 0 (default, no minimum interval)
            sensor.ReportInterval = 0;

            // Release the sensor reference
            sensor = null;
        }
    }

    /// <summary>
    /// Linux-specific implementation of gyroscope using the Linux Industrial I/O (IIO) subsystem.
    /// Reads angular velocity data directly from sysfs files in /sys/bus/iio/devices/.
    /// Uses polling since Linux doesn't provide event-based sensor APIs by default.
    /// Measures angular velocity in radians per second (rad/s) after applying scale factors.
    /// </summary>
    class LinuxGyroscope : AvaloniaGyroscope
    {
        /// <summary>
        /// Indicates whether a gyroscope was found in the Linux IIO subsystem.
        /// Returns true if a device with angular velocity sensor files was discovered.
        /// </summary>
        public override bool IsSupported => _devicePath is not null;

        private CancellationTokenSource? _cts;      // Token source for cancelling the polling loop
        private Task? _pollingTask;                 // Background task for polling sensor data
        private readonly string? _devicePath;       // Path to the IIO device directory (e.g., /sys/bus/iio/devices/iio:device0)

        /// <summary>
        /// Constructor - attempts to locate a gyroscope in the Linux IIO subsystem.
        /// Scans /sys/bus/iio/devices/ for a device that has angular velocity (anglvel) raw data files.
        /// </summary>
        public LinuxGyroscope()
        {
            if (Directory.Exists("/sys/bus/iio/devices/"))
            {
                // Search through all IIO devices to find one with gyroscope capability
                foreach (var dir in Directory.GetDirectories("/sys/bus/iio/devices/"))
                {
                    // Check for presence of all three angular velocity axis raw files
                    // (X, Y, Z axes are required for full 3D rotation tracking)
                    if (File.Exists(Path.Combine(dir, "in_anglvel_x_raw")) &&
                        File.Exists(Path.Combine(dir, "in_anglvel_y_raw")) &&
                        File.Exists(Path.Combine(dir, "in_anglvel_z_raw")))
                    {
                        _devicePath = dir;
                        break;  // Stop searching once we find the first gyroscope
                    }
                }
            }
        }

        /// <summary>
        /// Starts polling the Linux gyroscope.
        /// Launches a background task that continuously reads angular velocity data from sysfs files.
        /// </summary>
        /// <param name="sensorSpeed">Desired sensor update frequency (used to determine polling interval)</param>
        protected override void PlatformStart(SensorSpeed sensorSpeed)
        {
            if (_devicePath == null)
                throw new NotSupportedException("No gyroscope found in /sys/bus/iio/devices");

            // Create cancellation token source for stopping the polling loop
            _cts = new CancellationTokenSource();

            // Start the background polling task
            _pollingTask = Task.Run(() => PollingLoop(sensorSpeed, _cts.Token));
        }

        /// <summary>
        /// Stops polling the Linux gyroscope.
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
        /// Main polling loop that continuously reads gyroscope/angular velocity data from sysfs files.
        /// Runs on a background thread until cancellation is requested.
        /// </summary>
        /// <param name="sensorSpeed">Sensor speed (used to determine polling delay)</param>
        /// <param name="token">Cancellation token to stop the polling loop</param>
        private void PollingLoop(SensorSpeed sensorSpeed, CancellationToken token)
        {
            if (_devicePath == null)
                throw new NotSupportedException("No gyroscope found in /sys/bus/iio/devices");

            // Read scale factor (converts raw ADC values to physical units - radians per second)
            double scale = 1.0;
            var scalePath = Path.Combine(_devicePath, "in_anglvel_scale");
            if (File.Exists(scalePath))
            {
                var s = File.ReadAllText(scalePath).Trim();
                scale = double.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
            }

            // Continue polling until cancellation is requested
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Read raw angular velocity values from sysfs and apply scale factor
                    // to get physical readings in radians per second
                    double x = ReadRaw("in_anglvel_x_raw") * scale;
                    double y = ReadRaw("in_anglvel_y_raw") * scale;
                    double z = ReadRaw("in_anglvel_z_raw") * scale;

                    // Create MAUI gyroscope data and raise the ReadingChanged event
                    var data = new GyroscopeData(x, y, z);
                    RaiseReadingChanged(data);
                }
                catch (Exception ex)
                {
                    // Log errors but continue polling to maintain sensor functionality
                    Console.Error.WriteLine($"Error reading accelerometer: {ex.Message}");
                }

                // Wait for the specified interval before the next reading
                // Typical gyroscopes support 50-400 Hz, adjust based on sensorSpeed
                Thread.Sleep((int)sensorSpeed.ToPlatform());
            }
        }

        /// <summary>
        /// Reads a raw value from a sysfs file in the IIO device directory.
        /// </summary>
        /// <param name="filename">The sysfs filename to read (e.g., "in_anglvel_x_raw")</param>
        /// <returns>The raw integer value read from the file</returns>
        private double ReadRaw(string filename)
        {
            if (_devicePath == null)
                throw new NotSupportedException("No gyroscope found in /sys/bus/iio/devices");

            var path = Path.Combine(_devicePath, filename);
            var text = File.ReadAllText(path).Trim();
            // Parse with invariant culture to handle decimal separators consistently
            // (Linux uses '.' while some cultures use ',' as decimal separator)
            return double.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}