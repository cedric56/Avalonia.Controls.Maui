using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using System.Runtime.Versioning;
using Windows.Devices.Sensors;
using Sensor = Windows.Devices.Sensors.Accelerometer;

namespace Avalonia.Controls.Maui.Essentials
{
    /// <summary>
    /// Base platform-agnostic implementation for accelerometer.
    /// Provides default behavior for unsupported platforms - throws feature not supported exceptions.
    /// Platform-specific implementations override the virtual methods to provide actual sensor functionality.
    /// The accelerometer measures acceleration forces in m/s² including gravity.
    /// </summary>
    partial class AvaloniaAccelerometer
    {
        /// <summary>
        /// Indicates whether the accelerometer is supported on the current platform.
        /// Base implementation returns false, indicating no support.
        /// Platform-specific implementations override this to return actual support status.
        /// </summary>
        public override bool IsSupported => false;

        /// <summary>
        /// Platform-specific implementation to start accelerometer monitoring.
        /// Base implementation throws FeatureNotSupportedException.
        /// </summary>
        /// <param name="sensorSpeed">Desired sensor update speed/precision</param>
        protected override void PlatformStart(SensorSpeed sensorSpeed) =>
            throw new FeatureNotSupportedException();

        /// <summary>
        /// Platform-specific implementation to stop accelerometer monitoring.
        /// Base implementation throws FeatureNotSupportedException.
        /// </summary>
        protected override void PlatformStop() =>
            throw new FeatureNotSupportedException();
    }

    /// <summary>
    /// Windows-specific implementation of accelerometer using the UWP Windows.Devices.Sensors.Accelerometer API.
    /// Supported on Windows 10 build 10240 and later.
    /// Measures acceleration forces in m/s² including gravity.
    /// Note: Values are negated to align coordinate systems between Windows and MAUI.
    /// </summary>
    [SupportedOSPlatform("windows10.0.10240")]
    class WindowsAccelerometer : AvaloniaAccelerometer
    {
        // Keep a reference to the sensor so we can clean up and stop the same instance
        Sensor? sensor;

        /// <summary>
        /// Gets the default accelerometer sensor on the Windows device.
        /// Returns null if no accelerometer is present.
        /// </summary>
        internal static Sensor Sensor =>
            Sensor.GetDefault();

        /// <summary>
        /// Indicates whether an accelerometer is available on this Windows device.
        /// Returns true if the default sensor exists, false otherwise.
        /// </summary>
        public override bool IsSupported => Sensor != null;

        /// <summary>
        /// Starts the Windows accelerometer with the specified sensor speed.
        /// Configures the report interval based on the requested speed and minimum sensor capabilities.
        /// </summary>
        /// <param name="sensorSpeed">Desired sensor update frequency/precision</param>
        protected override void PlatformStart(SensorSpeed sensorSpeed)
        {
            // Convert SensorSpeed enum to interval in milliseconds
            var interval = sensorSpeed.ToPlatform();

            // Get the default accelerometer sensor
            sensor = Sensor;

            // Set the report interval - use the larger of requested interval or sensor's minimum
            // This respects both the app's desired update rate and the sensor's hardware limitations
            sensor.ReportInterval = sensor.MinimumReportInterval >= interval ? sensor.MinimumReportInterval : interval;

            // Subscribe to reading changed events
            sensor.ReadingChanged += DataUpdated;
        }

        /// <summary>
        /// Event handler for Windows accelerometer readings.
        /// Converts Windows sensor data to MAUI format and forwards to the base implementation.
        /// Note: Values are negated to align coordinate systems between Windows and MAUI.
        /// Windows uses a different axis orientation than MAUI's expected coordinate system.
        /// </summary>
        /// <param name="sender">The accelerometer sensor object</param>
        /// <param name="e">Event args containing the accelerometer reading</param>
        void DataUpdated(object sender, AccelerometerReadingChangedEventArgs e)
        {
            var reading = e.Reading;
            // Negate X and Y values to convert from Windows coordinate system to MAUI coordinate system
            // Windows and MAUI use different axis orientations for acceleration vectors
            var data = new AccelerometerData(reading.AccelerationX * -1, reading.AccelerationY * -1, reading.AccelerationZ * -1);
            OnChanged(new AccelerometerChangedEventArgs(data));
        }

        /// <summary>
        /// Stops the Windows accelerometer.
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
    /// Linux-specific implementation of accelerometer using the Linux Industrial I/O (IIO) subsystem.
    /// Reads accelerometer data directly from sysfs files in /sys/bus/iio/devices/.
    /// Uses polling since Linux doesn't provide event-based sensor APIs by default.
    /// Measures acceleration forces in m/s² after applying scale factors.
    /// </summary>
    class LinuxAccelerometer : AvaloniaAccelerometer
    {
        private CancellationTokenSource? _cts;      // Token source for cancelling the polling loop
        private Task? _pollingTask;                 // Background task for polling sensor data
        private readonly string? _devicePath;       // Path to the IIO device directory (e.g., /sys/bus/iio/devices/iio:device0)

        /// <summary>
        /// Indicates whether an accelerometer was found in the Linux IIO subsystem.
        /// Returns true if a device with accelerometer files was discovered.
        /// </summary>
        public override bool IsSupported => _devicePath != null;

        /// <summary>
        /// Constructor - attempts to locate an accelerometer device in the Linux IIO subsystem.
        /// Scans /sys/bus/iio/devices/ for a device that has accelerometer raw data files.
        /// Only runs on Linux operating systems.
        /// </summary>
        public LinuxAccelerometer()
        {
            if (OperatingSystem.IsLinux())
            {
                if (Directory.Exists("/sys/bus/iio/devices/"))
                {
                    // Search through all IIO devices to find one with accelerometer capability
                    foreach (var dir in Directory.GetDirectories("/sys/bus/iio/devices/"))
                    {
                        // Check for the presence of accelerometer raw data file (X-axis is a good indicator)
                        if (File.Exists(Path.Combine(dir, "in_accel_x_raw")))
                        {
                            _devicePath = dir;
                            break;  // Stop searching once we find the first accelerometer
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Starts polling the Linux accelerometer.
        /// Launches a background task that continuously reads sensor data from sysfs files.
        /// </summary>
        /// <param name="sensorSpeed">Desired sensor update frequency (used to determine polling interval)</param>
        protected override void PlatformStart(SensorSpeed sensorSpeed)
        {
            if (_devicePath == null)
                throw new NotSupportedException("No accelerometer found in /sys/bus/iio/devices");

            // Convert SensorSpeed to polling interval in milliseconds
            var interval = sensorSpeed.ToPlatform();

            // Create cancellation token source for stopping the polling loop
            _cts = new CancellationTokenSource();

            // Start the background polling task
            _pollingTask = Task.Run(() => PollingLoop(sensorSpeed, _cts.Token));
        }

        /// <summary>
        /// Stops polling the Linux accelerometer.
        /// Cancels the background task and waits for it to complete.
        /// </summary>
        protected async override void PlatformStop()
        {
            if (_devicePath is not null)
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
        }

        /// <summary>
        /// Main polling loop that continuously reads accelerometer data from sysfs files.
        /// Runs on a background thread until cancellation is requested.
        /// </summary>
        /// <param name="sensorSpeed">Sensor speed (used to determine polling delay)</param>
        /// <param name="token">Cancellation token to stop the polling loop</param>
        private void PollingLoop(SensorSpeed sensorSpeed, CancellationToken token)
        {
            if (_devicePath == null)
                throw new NotSupportedException("No accelerometer found in /sys/bus/iio/devices");

            // Determine polling interval from sensor speed
            var interval = sensorSpeed.ToPlatform();

            // Construct paths to raw data files for each axis
            string xRaw = Path.Combine(_devicePath, "in_accel_x_raw");
            string yRaw = Path.Combine(_devicePath, "in_accel_y_raw");
            string zRaw = Path.Combine(_devicePath, "in_accel_z_raw");

            // Construct paths to scale files (convert raw ADC values to physical units)
            string xScaleFile = Path.Combine(_devicePath, "in_accel_x_scale");
            string yScaleFile = Path.Combine(_devicePath, "in_accel_y_scale");
            string zScaleFile = Path.Combine(_devicePath, "in_accel_z_scale");

            // Read scale factors (conversion from raw integer to g-force or m/s²)
            // Default to 1.0 if scale file doesn't exist (assume raw values are already in physical units)
            double xScale = File.Exists(xScaleFile) ? double.Parse(File.ReadAllText(xScaleFile)) : 1.0;
            double yScale = File.Exists(yScaleFile) ? double.Parse(File.ReadAllText(yScaleFile)) : 1.0;
            double zScale = File.Exists(zScaleFile) ? double.Parse(File.ReadAllText(zScaleFile)) : 1.0;

            // Continue polling until cancellation is requested
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Read raw integer values from sysfs and convert to physical units
                    double x = int.Parse(File.ReadAllText(xRaw)) * xScale;
                    double y = int.Parse(File.ReadAllText(yRaw)) * yScale;
                    double z = int.Parse(File.ReadAllText(zRaw)) * zScale;

                    // Create MAUI accelerometer data and raise the Changed event
                    var data = new AccelerometerData(x, y, z);
                    OnChanged(new AccelerometerChangedEventArgs(data));
                }
                catch (Exception ex)
                {
                    // Log errors but continue polling to maintain sensor functionality
                    Console.Error.WriteLine($"Error reading accelerometer: {ex.Message}");
                }

                // Wait for the specified interval before the next reading
                Thread.Sleep((int)interval);
            }
        }
    }
}