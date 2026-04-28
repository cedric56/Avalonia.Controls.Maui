using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using System.Runtime.Versioning;
using Windows.Devices.Sensors;
using Sensor = Windows.Devices.Sensors.OrientationSensor;

namespace Avalonia.Controls.Maui.Essentials
{
    /// <summary>
    /// Base platform-agnostic implementation for orientation sensor (absolute device orientation).
    /// Provides default behavior for unsupported platforms - throws feature not supported exceptions.
    /// Platform-specific implementations override the virtual methods to provide actual sensor functionality.
    /// The orientation sensor provides device rotation in 3D space using quaternion representation
    /// (W, X, Y, Z components) relative to Earth's coordinate system.
    /// </summary>
    partial class AvaloniaOrientationSensor
    {
        /// <summary>
        /// Gets a value indicating whether reading the orientation sensor is supported on this device.
        /// Base implementation returns false, indicating no support.
        /// Platform-specific implementations override this to return actual support status.
        /// </summary>
        public override bool IsSupported => false;

        /// <summary>
        /// Platform-specific implementation to start orientation sensor monitoring.
        /// Base implementation throws FeatureNotSupportedException.
        /// </summary>
        /// <param name="sensorSpeed">Desired sensor update speed/precision</param>
        protected override void PlatformStart(SensorSpeed sensorSpeed)
        {
            throw new FeatureNotSupportedException();
        }

        /// <summary>
        /// Platform-specific implementation to stop orientation sensor monitoring.
        /// Base implementation throws FeatureNotSupportedException.
        /// </summary>
        protected override void PlatformStop()
        {
            throw new FeatureNotSupportedException();
        }
    }

    /// <summary>
    /// Linux-specific implementation of orientation sensor using the Linux Industrial I/O (IIO) subsystem.
    /// Reads rotation vector (orientation) data directly from sysfs files in /sys/bus/iio/devices/.
    /// Uses polling since Linux doesn't provide event-based sensor APIs by default.
    /// Provides quaternion-based orientation (W, X, Y, Z) representing device rotation.
    /// Note: Linux IIO subsystem provides rotation vectors that may need interpretation for absolute orientation.
    /// </summary>
    class LinuxOrientationSensor : AvaloniaOrientationSensor
    {
        /// <summary>
        /// Indicates whether an orientation sensor was found in the Linux IIO subsystem.
        /// Returns true if a device with rotation vector files was discovered.
        /// </summary>
        public override bool IsSupported => _devicePath is not null;

        private CancellationTokenSource? _cts;      // Token source for cancelling the polling loop
        private Task? _pollingTask;                 // Background task for polling sensor data
        private readonly string? _devicePath;       // Path to the IIO device directory (e.g., /sys/bus/iio/devices/iio:device0)

        /// <summary>
        /// Constructor - attempts to locate an orientation sensor in the Linux IIO subsystem.
        /// Scans /sys/bus/iio/devices/ for a device that has rotation vector (rotvec) raw data files.
        /// </summary>
        public LinuxOrientationSensor()
        {
            if (Directory.Exists("/sys/bus/iio/devices/"))
            {
                // Search through all IIO devices to find one with orientation sensor capability
                foreach (var dir in Directory.GetDirectories("/sys/bus/iio/devices/"))
                {
                    // Check for presence of rotation vector axis raw files
                    // Rotation vector describes device orientation in 3D space
                    if (File.Exists(Path.Combine(dir, "in_rotvec_x_raw")) &&
                        File.Exists(Path.Combine(dir, "in_rotvec_y_raw")) &&
                        File.Exists(Path.Combine(dir, "in_rotvec_z_raw")))
                    {
                        _devicePath = dir;
                        break;  // Stop searching once we find the first orientation sensor
                    }
                }
            }
        }

        /// <summary>
        /// Starts polling the Linux orientation sensor.
        /// Launches a background task that continuously reads rotation vector data from sysfs files.
        /// </summary>
        /// <param name="sensorSpeed">Desired sensor update frequency (used to determine polling interval)</param>
        protected override void PlatformStart(SensorSpeed sensorSpeed)
        {
            if (_devicePath == null)
                throw new NotSupportedException("No orientation sensor found in /sys/bus/iio/devices");

            // Create cancellation token source for stopping the polling loop
            _cts = new CancellationTokenSource();

            // Start the background polling task
            _pollingTask = Task.Run(() => PollingLoop(sensorSpeed, _cts.Token));
        }

        /// <summary>
        /// Stops polling the Linux orientation sensor.
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
        /// Main polling loop that continuously reads orientation/rotation vector data from sysfs files.
        /// Runs on a background thread until cancellation is requested.
        /// </summary>
        /// <param name="sensorSpeed">Sensor speed (used to determine polling delay)</param>
        /// <param name="token">Cancellation token to stop the polling loop</param>
        private void PollingLoop(SensorSpeed sensorSpeed, CancellationToken token)
        {
            if (_devicePath == null)
                throw new NotSupportedException("No orientation sensor found in /sys/bus/iio/devices");

            // Read scale factor (converts raw ADC values to normalized quaternion components)
            double scale = 1.0;
            var scalePath = Path.Combine(_devicePath, "in_rotvec_scale");
            if (File.Exists(scalePath))
            {
                scale = double.Parse(File.ReadAllText(scalePath).Trim(),
                    System.Globalization.CultureInfo.InvariantCulture);  // Use invariant culture for decimal parsing
            }

            // Continue polling until cancellation is requested
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Read quaternion components from sysfs files
                    // W component represents the scalar/real part
                    // X, Y, Z represent the vector components for axis-angle representation
                    double w = ReadRaw("in_rotvec_w_raw");
                    double x = ReadRaw("in_rotvec_x_raw");
                    double y = ReadRaw("in_rotvec_y_raw");
                    double z = ReadRaw("in_rotvec_z_raw");

                    // Create MAUI orientation sensor data with scaled quaternion components
                    // The W component is typically the real part, X/Y/Z are imaginary components
                    var data = new OrientationSensorData(w * scale, x * scale, y * scale, z * scale);
                    RaiseReadingChanged(data);
                }
                catch (Exception ex)
                {
                    // Log errors but continue polling to maintain sensor functionality
                    Console.Error.WriteLine($"Error reading orientation sensor : {ex.Message}");
                }

                // Wait for the specified interval before the next reading
                // Typical orientation sensors support 50-200 Hz, adjust based on sensorSpeed
                Thread.Sleep((int)sensorSpeed.ToPlatform());
            }
        }

        /// <summary>
        /// Reads a raw value from a sysfs file in the IIO device directory.
        /// Handles file reading and parsing with invariant culture for consistent decimal parsing.
        /// </summary>
        /// <param name="filename">The sysfs filename to read (e.g., "in_rotvec_x_raw")</param>
        /// <returns>The raw integer or floating-point value read from the file</returns>
        private double ReadRaw(string filename)
        {
            if (_devicePath == null)
                throw new NotSupportedException("No orientation sensor found in /sys/bus/iio/devices");

            var path = Path.Combine(_devicePath, filename);
            if (!File.Exists(path)) return 0;

            // Read and parse the value using invariant culture to handle decimal separators correctly
            // (Linux uses '.' while some cultures use ',' as decimal separator)
            var text = File.ReadAllText(path).Trim();
            return double.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// Windows-specific implementation of orientation sensor using the UWP Windows.Devices.Sensors.OrientationSensor API.
    /// Supported on Windows 10 build 10240 and later.
    /// Provides absolute device orientation as a quaternion (W, X, Y, Z) relative to Earth's coordinate system.
    /// The quaternion represents rotation without gimbal lock issues.
    /// </summary>
    [SupportedOSPlatform("windows10.0.10240")]
    class WindowsOrientationSensor : AvaloniaOrientationSensor
    {
        /// <summary>
        /// Indicates whether an orientation sensor is available on this Windows device.
        /// Returns true if the default sensor exists, false otherwise.
        /// </summary>
        public override bool IsSupported => DefaultSensor != null;

        // Keep a reference to the sensor so we can clean up and stop the same instance
        Sensor? sensor;

        /// <summary>
        /// Gets the default orientation sensor on the Windows device.
        /// Returns null if no orientation sensor is present.
        /// </summary>
        static Sensor DefaultSensor =>
            Sensor.GetDefault();

        /// <summary>
        /// Starts the Windows orientation sensor with the specified sensor speed.
        /// Configures the report interval based on the requested speed and minimum sensor capabilities.
        /// </summary>
        /// <param name="sensorSpeed">Desired sensor update frequency/precision</param>
        protected override void PlatformStart(SensorSpeed sensorSpeed)
        {
            // Convert SensorSpeed enum to interval in milliseconds
            var interval = sensorSpeed.ToPlatform();

            // Get the default orientation sensor
            sensor = DefaultSensor;

            // Set the report interval - use the larger of requested interval or sensor's minimum
            // This respects both the app's desired update rate and the sensor's hardware limitations
            sensor.ReportInterval = sensor.MinimumReportInterval >= interval ? sensor.MinimumReportInterval : interval;

            // Subscribe to reading changed events
            sensor.ReadingChanged += DataUpdated;
        }

        /// <summary>
        /// Event handler for Windows orientation sensor readings.
        /// Extracts quaternion data (X, Y, Z, W) and raises the ReadingChanged event.
        /// Quaternion components represent device rotation in 3D space.
        /// Note: Windows provides quaternion in order (X, Y, Z, W) which matches MAUI's expected format.
        /// </summary>
        /// <param name="sender">The orientation sensor object</param>
        /// <param name="e">Event args containing the orientation reading</param>
        void DataUpdated(object sender, OrientationSensorReadingChangedEventArgs e)
        {
            var reading = e.Reading;
            // Create orientation data from Windows quaternion components
            // Windows provides X, Y, Z, W quaternion components (same order as MAUI expects)
            var data = new OrientationSensorData(reading.Quaternion.X, reading.Quaternion.Y, reading.Quaternion.Z, reading.Quaternion.W);
            RaiseReadingChanged(data);
        }

        /// <summary>
        /// Stops the Windows orientation sensor.
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
}