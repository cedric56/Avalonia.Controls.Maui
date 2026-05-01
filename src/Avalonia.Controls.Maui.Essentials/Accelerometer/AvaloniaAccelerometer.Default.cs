using Microsoft.Maui.Devices.Sensors;
using System.Globalization;
using System.Reflection.PortableExecutable;

namespace Avalonia.Controls.Maui.Essentials;

/// <summary>
/// Linux-specific implementation of accelerometer using the Linux Industrial I/O (IIO) subsystem.
/// Reads accelerometer data directly from sysfs files in /sys/bus/iio/devices/.
/// Uses polling since Linux doesn't provide event-based sensor APIs by default.
/// Measures acceleration forces in m/s² after applying scale factors.
/// </summary>
partial class AvaloniaAccelerometer
{
    private CancellationTokenSource? _cts;      // Token source for cancelling the polling loop
    private Task? _pollingTask;                 // Background task for polling sensor data
    private readonly string? _devicePath;       // Path to the IIO device directory (e.g., /sys/bus/iio/devices/iio:device0)
    private readonly object _lock = new();

    bool PlatformIsSupported() => _devicePath != null;

    /// <summary>
    /// Constructor - attempts to locate an accelerometer device in the Linux IIO subsystem.
    /// Scans /sys/bus/iio/devices/ for a device that has accelerometer raw data files.
    /// Only runs on Linux operating systems.
    /// </summary>
    public AvaloniaAccelerometer()
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
    void PlatformStart(SensorSpeed sensorSpeed)
    {   
        // Create cancellation token source for stopping the polling loop
        _cts = new CancellationTokenSource();

        // Start the background polling task
        _pollingTask = RunPollingAsync(sensorSpeed, _cts.Token);
    }

    /// <summary>
    /// Stops polling the Linux accelerometer.
    /// Cancels the background task and waits for it to complete.
    /// </summary>
    void PlatformStop()
    {
        CancellationTokenSource? cts;
        Task? task;

        lock (_lock)
        {
            cts = _cts;
            task = _pollingTask;

            _cts = null;
            _pollingTask = null;
        }

        cts?.Cancel();

        if (task != null)
        {
            // Fire-and-forget wait with timeout - best compromise
            _ = Task.Run(async () =>
            {
                try
                {
                    await task.WaitAsync(TimeSpan.FromSeconds(2));
                }
                catch { }
                finally
                {
                    cts?.Dispose();
                }
            });
        }
        else
        {
            cts?.Dispose();
        }
    }

    /// <summary>
    /// Main polling loop that continuously reads accelerometer data from sysfs files.
    /// Runs on a background thread until cancellation is requested.
    /// </summary>
    /// <param name="sensorSpeed">Sensor speed (used to determine polling delay)</param>
    /// <param name="token"></param>
    private async Task RunPollingAsync(SensorSpeed sensorSpeed, CancellationToken token)
    {
        if (_devicePath == null) return;

        var interval = GetInterval(sensorSpeed);

        string xRaw = Path.Combine(_devicePath, "in_accel_x_raw");
        string yRaw = Path.Combine(_devicePath, "in_accel_y_raw");
        string zRaw = Path.Combine(_devicePath, "in_accel_z_raw");

        // Construct paths to scale files (convert raw ADC values to physical units)
        string xScaleFile = Path.Combine(_devicePath, "in_accel_x_scale");
        string yScaleFile = Path.Combine(_devicePath, "in_accel_y_scale");
        string zScaleFile = Path.Combine(_devicePath, "in_accel_z_scale");

        // Read scale factors (conversion from raw integer to g-force or m/s²)
        // Default to 1.0 if scale file doesn't exist (assume raw values are already in physical units)
        double xScale = File.Exists(xScaleFile) && double.TryParse(File.ReadAllText(xScaleFile), CultureInfo.InvariantCulture, out var xScaleVal) ? xScaleVal : 1.0;
        double yScale = File.Exists(yScaleFile) && double.TryParse(File.ReadAllText(yScaleFile), CultureInfo.InvariantCulture, out var yScaleVal) ? yScaleVal : 1.0;
        double zScale = File.Exists(zScaleFile) && double.TryParse(File.ReadAllText(zScaleFile), CultureInfo.InvariantCulture, out var zScaleVal) ? zScaleVal : 1.0;

        while (!token.IsCancellationRequested)
        {
            // Read raw integer values from sysfs and convert to physical units
            //AccelerometerData in MAUI expects values in g - force(where 1g ≈ 9.81 m / s²).
            //Most IIO accelerometers output m / s² after applying the scale.
            //→ You must divide by 9.80665.
            double x = int.TryParse(File.ReadAllText(xRaw), out var xVal) ? (xVal * xScale) / 9.80665 : 0;
            double y = int.TryParse(File.ReadAllText(yRaw), out var yVal) ? (yVal * yScale) / 9.80665 : 0;
            double z = int.TryParse(File.ReadAllText(zRaw), out var zVal) ? (zVal * zScale) / 9.80665 : 0;

            OnChanged(new AccelerometerChangedEventArgs(new AccelerometerData(x, y, z)));

            try
            {
                await Task.Delay(interval, token);
            }
            catch (OperationCanceledException)
            {
                break;  // Exit loop if cancellation is requested during delay
            }
        }
    }        

    private static int GetInterval(SensorSpeed speed) => speed switch
    {
        SensorSpeed.Default => 200,   // 5 Hz - Battery efficient, good for orientation
        SensorSpeed.UI => 50,         // 20 Hz - Smooth UI updates (66ms was fine too)
        SensorSpeed.Game => 20,       // 50 Hz - Better for game physics
        SensorSpeed.Fastest => 10,    // 100 Hz - Maximum practical rate on Linux
        _ => 100
    };
}