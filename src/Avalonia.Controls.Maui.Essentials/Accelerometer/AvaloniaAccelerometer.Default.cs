using Microsoft.Maui.Devices.Sensors;

namespace Avalonia.Controls.Maui.Essentials
{
    /// <summary>
    /// Linux-specific implementation of accelerometer using the Linux Industrial I/O (IIO) subsystem.
    /// Reads accelerometer data directly from sysfs files in /sys/bus/iio/devices/.
    /// Uses polling since Linux doesn't provide event-based sensor APIs by default.
    /// Measures acceleration forces in m/s² after applying scale factors.
    /// </summary>
    class LinuxAccelerometer : AccelerometerImplementation, IAccelerometer
    {
        private CancellationTokenSource? _cts;      // Token source for cancelling the polling loop
        private Task? _pollingTask;                 // Background task for polling sensor data
        private readonly string? _devicePath;       // Path to the IIO device directory (e.g., /sys/bus/iio/devices/iio:device0)

        
        private bool _isMonitoring;

        /// <summary>
        /// Indicates whether an accelerometer was found in the Linux IIO subsystem.
        /// Returns true if a device with accelerometer files was discovered.
        /// </summary>
        bool IAccelerometer.IsSupported => _devicePath != null;

        bool IAccelerometer.IsMonitoring => _isMonitoring;

        public new bool IsMonitoring => _isMonitoring;

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
        void IAccelerometer.Start(SensorSpeed sensorSpeed)
        {
            if (_devicePath == null)
                throw new NotSupportedException("No accelerometer found in /sys/bus/iio/devices");

            _isMonitoring = true;

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
        async void IAccelerometer.Stop()
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
                    finally
                    {
                        _isMonitoring = false;
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