using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using System.Runtime.Versioning;
using Windows.Devices.Sensors;
using Sensor = Windows.Devices.Sensors.Magnetometer;

namespace Avalonia.Controls.Maui.Essentials
{
    /// <summary>
    /// Base platform-agnostic implementation for magnetometer (magnetic field sensor).
    /// Provides default behavior for unsupported platforms - throws feature not supported exceptions.
    /// Platform-specific implementations override the virtual methods to provide actual sensor functionality.
    /// The magnetometer measures magnetic field strength in microteslas (μT) along X, Y, and Z axes.
    /// This includes the Earth's magnetic field plus any local magnetic interference.
    /// </summary>
    partial class AvaloniaMagnetometer
    {
        /// <summary>
        /// Indicates whether the magnetometer is supported on the current platform.
        /// Base implementation returns false, indicating no support.
        /// Platform-specific implementations override this to return actual support status.
        /// </summary>
        public override bool IsSupported => false;

        /// <summary>
        /// Platform-specific implementation to start magnetometer monitoring.
        /// Base implementation throws FeatureNotSupportedException.
        /// </summary>
        /// <param name="sensorSpeed">Desired sensor update speed/precision</param>
        protected override void PlatformStart(SensorSpeed sensorSpeed) =>
            throw new FeatureNotSupportedException();

        /// <summary>
        /// Platform-specific implementation to stop magnetometer monitoring.
        /// Base implementation throws FeatureNotSupportedException.
        /// </summary>
        protected override void PlatformStop() =>
            throw new FeatureNotSupportedException();
    }

    /// <summary>
    /// Windows-specific implementation of magnetometer using the UWP Windows.Devices.Sensors.Magnetometer API.
    /// Supported on Windows 10 build 10240 and later.
    /// Measures magnetic field strength in microteslas (μT) along X, Y, and Z axes.
    /// </summary>
    [SupportedOSPlatform("windows10.0.10240")]
    class WindowsMagnetometer : AvaloniaMagnetometer
    {
        /// <summary>
        /// Indicates whether a magnetometer is available on this Windows device.
        /// Returns true if the default sensor exists, false otherwise.
        /// </summary>
        public override bool IsSupported => DefaultSensor != null;

        // Keep a reference to the sensor so we can clean up and stop the same instance
        Sensor? sensor;

        /// <summary>
        /// Gets the default magnetometer sensor on the Windows device.
        /// Returns null if no magnetometer is present.
        /// </summary>
        static Sensor DefaultSensor =>
            Sensor.GetDefault();

        /// <summary>
        /// Starts the Windows magnetometer with the specified sensor speed.
        /// Configures the report interval based on the requested speed and minimum sensor capabilities.
        /// </summary>
        /// <param name="sensorSpeed">Desired sensor update frequency/precision</param>
        protected override void PlatformStart(SensorSpeed sensorSpeed)
        {
            // Convert SensorSpeed enum to interval in milliseconds
            var interval = sensorSpeed.ToPlatform();

            // Get the default magnetometer sensor
            sensor = DefaultSensor;

            // Set the report interval - use the larger of requested interval or sensor's minimum
            // This respects both the app's desired update rate and the sensor's hardware limitations
            sensor.ReportInterval = sensor.MinimumReportInterval >= interval ? sensor.MinimumReportInterval : interval;

            // Subscribe to reading changed events
            sensor.ReadingChanged += DataUpdated;
        }

        /// <summary>
        /// Event handler for Windows magnetometer readings.
        /// Extracts magnetic field data in microteslas (μT) and raises the ReadingChanged event.
        /// </summary>
        /// <param name="sender">The magnetometer sensor object</param>
        /// <param name="e">Event args containing the magnetometer reading</param>
        void DataUpdated(object sender, MagnetometerReadingChangedEventArgs e)
        {
            var reading = e.Reading;
            // Magnetic field values are in microteslas (μT)
            var data = new MagnetometerData(reading.MagneticFieldX, reading.MagneticFieldY, reading.MagneticFieldZ);
            RaiseReadingChanged(data);
        }

        /// <summary>
        /// Stops the Windows magnetometer.
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
    /// Linux-specific implementation of magnetometer using the Linux Industrial I/O (IIO) subsystem.
    /// Reads magnetic field data directly from sysfs files in /sys/bus/iio/devices/.
    /// Uses polling since Linux doesn't provide event-based sensor APIs by default.
    /// Measures magnetic field strength in microteslas (μT) after applying scale factors.
    /// </summary>
    class LinuxMagnetometer : AvaloniaMagnetometer
    {
        /// <summary>
        /// Indicates whether a magnetometer was found in the Linux IIO subsystem.
        /// Returns true if a device with magnetic field sensor files was discovered.
        /// </summary>
        public override bool IsSupported => _devicePath is not null;

        private CancellationTokenSource? _cts;      // Token source for cancelling the polling loop
        private Task? _pollingTask;                 // Background task for polling sensor data
        private readonly string? _devicePath;       // Path to the IIO device directory (e.g., /sys/bus/iio/devices/iio:device0)

        /// <summary>
        /// Constructor - attempts to locate a magnetometer in the Linux IIO subsystem.
        /// Scans /sys/bus/iio/devices/ for a device that has magnetic field (magn) raw data files.
        /// </summary>
        public LinuxMagnetometer()
        {
            if (Directory.Exists("/sys/bus/iio/devices/"))
            {
                // Search through all IIO devices to find one with magnetometer capability
                foreach (var dir in Directory.GetDirectories("/sys/bus/iio/devices/"))
                {
                    // Check for presence of all three magnetic field axis raw files
                    // (X, Y, Z axes are required for full 3D magnetic field measurement)
                    if (File.Exists(Path.Combine(dir, "in_magn_x_raw")) &&
                        File.Exists(Path.Combine(dir, "in_magn_y_raw")) &&
                        File.Exists(Path.Combine(dir, "in_magn_z_raw")))
                    {
                        _devicePath = dir;
                        break;  // Stop searching once we find the first magnetometer
                    }
                }
            }
        }

        /// <summary>
        /// Starts polling the Linux magnetometer.
        /// Launches a background task that continuously reads magnetic field data from sysfs files.
        /// </summary>
        /// <param name="sensorSpeed">Desired sensor update frequency (used to determine polling interval)</param>
        protected override void PlatformStart(SensorSpeed sensorSpeed)
        {
            if (_devicePath == null)
                throw new NotSupportedException("No magnetometer found in /sys/bus/iio/devices");

            // Create cancellation token source for stopping the polling loop
            _cts = new CancellationTokenSource();

            // Start the background polling task
            _pollingTask = Task.Run(() => PollingLoop(sensorSpeed, _cts.Token));
        }

        /// <summary>
        /// Stops polling the Linux magnetometer.
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
        /// Main polling loop that continuously reads magnetometer/magnetic field data from sysfs files.
        /// Runs on a background thread until cancellation is requested.
        /// </summary>
        /// <param name="sensorSpeed">Sensor speed (used to determine polling delay)</param>
        /// <param name="token">Cancellation token to stop the polling loop</param>
        private void PollingLoop(SensorSpeed sensorSpeed, CancellationToken token)
        {
            if (_devicePath == null)
                throw new NotSupportedException("No magnetometer found in /sys/bus/iio/devices");

            // Read scale factor (converts raw ADC values to physical units - microteslas)
            double scale = 1.0;
            var scalePath = Path.Combine(_devicePath, "in_magn_scale");
            if (File.Exists(scalePath))
                scale = double.Parse(File.ReadAllText(scalePath).Trim(),
                    System.Globalization.CultureInfo.InvariantCulture);  // Use invariant culture for decimal parsing

            // Continue polling until cancellation is requested
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Read raw magnetic field values from sysfs and apply scale factor
                    // to get physical readings in microteslas (μT)
                    double x = ReadRaw("in_magn_x_raw") * scale;
                    double y = ReadRaw("in_magn_y_raw") * scale;
                    double z = ReadRaw("in_magn_z_raw") * scale;

                    // Create MAUI magnetometer data and raise the ReadingChanged event
                    var data = new MagnetometerData(x, y, z);
                    RaiseReadingChanged(data);
                }
                catch (Exception ex)
                {
                    // Log errors but continue polling to maintain sensor functionality
                    Console.Error.WriteLine($"Error reading magnetometer : {ex.Message}");
                }

                // Wait for the specified interval before the next reading
                // Typical magnetometers support 10-100 Hz, adjust based on sensorSpeed
                Thread.Sleep((int)sensorSpeed.ToPlatform());
            }
        }

        /// <summary>
        /// Reads a raw value from a sysfs file in the IIO device directory.
        /// Handles file reading and parsing with invariant culture for consistent decimal parsing.
        /// </summary>
        /// <param name="filename">The sysfs filename to read (e.g., "in_magn_x_raw")</param>
        /// <returns>The raw integer or floating-point value read from the file</returns>
        private double ReadRaw(string filename)
        {
            if (_devicePath == null)
                throw new NotSupportedException("No magnetometer found in /sys/bus/iio/devices");

            var path = Path.Combine(_devicePath, filename);
            if (!File.Exists(path)) return 0;

            // Read and parse the value using invariant culture to handle decimal separators correctly
            // (Linux uses '.' while some cultures use ',' as decimal separator)
            var text = File.ReadAllText(path).Trim();
            return double.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}