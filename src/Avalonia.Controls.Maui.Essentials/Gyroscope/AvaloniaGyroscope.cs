using Microsoft.Maui.Devices.Sensors;

namespace Avalonia.Controls.Maui.Essentials
{
    /// <summary>
    /// Platform-agnostic implementation of IGyroscope for Avalonia environments.
    /// This partial class manages the gyroscope sensor lifecycle and event handling,
    /// while delegating platform-specific implementation to the partial class counterpart.
    /// The gyroscope measures angular velocity (rotation rate) in radians per second (rad/s)
    /// around the device's three axes (X: pitch, Y: roll, Z: yaw).
    /// </summary>
    public partial class AvaloniaGyroscope : AvaloniaSensor, IGyroscope
    {
        /// <summary>
        /// Exception message to display when attempting to start an already monitoring sensor.
        /// Used by the base AvaloniaSensor class for consistent error messaging.
        /// </summary>
        protected override string MonitoringExceptionMessage => "Gyroscope has already been started.";

        /// <summary>
        /// Occurs when the gyroscope reading changes.
        /// Provides angular velocity data for X, Y, and Z axes in radians per second (rad/s).
        /// X-axis: rotation rate around the device's horizontal axis (pitch/forward-back tilt)
        /// Y-axis: rotation rate around the device's vertical axis (roll/left-right tilt)
        /// Z-axis: rotation rate around the device's perpendicular axis (yaw/compass rotation)
        /// </summary>
        public event EventHandler<GyroscopeChangedEventArgs>? ReadingChanged;

        /// <summary>
        /// Raises the ReadingChanged event with the provided gyroscope data.
        /// This method is called by the platform-specific implementation when new
        /// angular velocity readings are received from the underlying platform APIs.
        /// </summary>
        /// <param name="data">The gyroscope data containing angular velocity measurements
        /// for X, Y, and Z axes in radians per second (rad/s).
        /// Typical ranges: -10 to +10 rad/s for normal device rotation,
        /// though sensors may support up to 100 rad/s in either direction.</param>
        protected void RaiseReadingChanged(GyroscopeData data)
        {
            // Create event arguments with the gyroscope reading
            // Pass null as the sender because this method is typically called from:
            // 1. Static JSExport methods in WebAssembly (no instance context)
            // 2. Platform-specific code that doesn't have a meaningful sender reference
            ReadingChanged?.Invoke(null, new GyroscopeChangedEventArgs(data));
        }
    }
}