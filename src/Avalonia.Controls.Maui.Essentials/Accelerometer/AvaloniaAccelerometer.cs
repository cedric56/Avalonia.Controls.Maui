using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;

namespace Avalonia.Controls.Maui.Essentials;

partial class AvaloniaAccelerometer : IAccelerometer
{
    private const double ForceThreshold = 0.8; // movement sensitivity
    private const int MaxPauseMs = 200;
    private const int MinChanges = 3;
    private const int MaxDurationMs = 500;

    private double _lastX;
    private double _lastY;
    private double _lastZ;
    private double _lastMagnitude;

    private DateTimeOffset _firstChange = DateTimeOffset.MinValue;
    private DateTimeOffset _lastChange = DateTimeOffset.MinValue;
    private int _changeCount;

    /// <summary>
    /// Indicates whether the accelerometer is currently monitoring sensor readings.
    /// Public version that hides the base implementation (using 'new' keyword).
    /// </summary>
    public bool IsMonitoring { get; private set; }

    /// <summary>
    /// Indicates whether the accelerometer is currently supportd sensor.
    /// Public version that hides the base implementation (using 'new' keyword).
    /// Returning value define by platform-specific implementation of PlatformIsSupported property.
    /// </summary>
    public bool IsSupported => PlatformIsSupported();

    public event EventHandler<AccelerometerChangedEventArgs>? ReadingChanged;
    public event EventHandler? ShakeDetected;

    /// <summary>
    /// Starts monitoring accelerometer readings at the specified speed/frequency.
    /// </summary>
    /// <param name="sensorSpeed">
    /// The desired speed/update frequency of the sensor readings.
    /// </param>
    /// <exception cref="FeatureNotSupportedException">
    /// Thrown when the device doesn't have an accelerometer or sensor access is unavailable.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if Start() is called when the accelerometer is already monitoring.
    /// Call Stop() first before restarting.
    /// </exception>
    public void Start(SensorSpeed sensorSpeed)
    {
        if (!IsSupported)
            throw new FeatureNotSupportedException();

        if (IsMonitoring)
            throw new InvalidOperationException("Accelerometer has already been started.");

        IsMonitoring = true;

        try
        {
            PlatformStart(sensorSpeed);
        }
        catch
        {
            IsMonitoring = false;
            throw;
        }
    }

    /// <summary>
    /// Stops monitoring accelerometer readings and releases sensor resources.
    /// </summary>
    /// <exception cref="FeatureNotSupportedException">
    /// Thrown when the device doesn't have an accelerometer or sensor access is unavailable.
    /// </exception>
    public void Stop()
    {
        if (!IsSupported)
            throw new FeatureNotSupportedException();

        if (!IsMonitoring)
            return;

        IsMonitoring = false;

        try
        {
            PlatformStop();
        }
        catch
        {
            IsMonitoring = true;
            throw;
        }
    }

    void OnChanged(AccelerometerChangedEventArgs args)
    {
        ReadingChanged?.Invoke(this, args);

        var x = args.Reading.Acceleration.X;
        var y = args.Reading.Acceleration.Y;
        var z = args.Reading.Acceleration.Z;

        var now = DateTimeOffset.UtcNow;

        // 1. Compute magnitude (physics-based signal)
        var magnitude = Math.Sqrt(x * x + y * y + z * z);

        // 2. Compute delta (removes gravity bias issues)
        var delta = Math.Abs(magnitude - _lastMagnitude);

        _lastMagnitude = magnitude;

        if (delta < ForceThreshold)
            return;

        // 3. Compute direction change strength (Uno idea but simplified)
        var directionChange =
            Math.Abs(x - _lastX) +
            Math.Abs(y - _lastY) +
            Math.Abs(z - _lastZ);

        _lastX = x;
        _lastY = y;
        _lastZ = z;

        if (directionChange < ForceThreshold)
            return;

        // 4. First trigger
        if (_firstChange == DateTimeOffset.MinValue)
        {
            _firstChange = now;
            _lastChange = now;
        }

        var timeSinceLast = (now - _lastChange).TotalMilliseconds;

        // 5. Too slow → reset
        if (timeSinceLast > MaxPauseMs)
        {
            Reset();
            return;
        }

        _lastChange = now;
        _changeCount++;

        // 6. Valid shake window
        if (_changeCount >= MinChanges)
        {
            var duration = (now - _firstChange).TotalMilliseconds;

            if (duration <= MaxDurationMs)
            {
                Reset();
                ShakeDetected?.Invoke(this, EventArgs.Empty);
            }
        }

        void Reset()
        {
            _firstChange = DateTimeOffset.MinValue;
            _lastChange = DateTimeOffset.MinValue;
            _changeCount = 0;
        }
    }
}
