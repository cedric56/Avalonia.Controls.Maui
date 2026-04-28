using Microsoft.Maui.Devices.Sensors;

namespace Avalonia.Controls.Maui.Essentials
{
    /// <summary>
    /// Platform-agnostic implementation of IOrientationSensor for Avalonia environments.
    /// This partial class manages the orientation sensor lifecycle and event handling,
    /// while delegating platform-specific implementation to the partial class counterpart.
    /// The orientation sensor provides absolute device rotation in 3D space using quaternion
    /// representation (W, X, Y, Z components) relative to Earth's coordinate system.
    /// Quaternions provide singularity-free 3D rotation without gimbal lock issues.
    /// </summary>
    public partial class AvaloniaOrientationSensor : AvaloniaSensor, IOrientationSensor
    {
        /// <summary>
        /// Occurs when the orientation reading changes.
        /// Provides quaternion-based orientation data (X, Y, Z, W) representing the device's
        /// absolute rotation in 3D space relative to Earth's coordinate system (north, east, up).
        /// 
        /// Quaternion components:
        /// - X: rotation around the X-axis (pitch/forward-back tilt)
        /// - Y: rotation around the Y-axis (roll/left-right tilt)
        /// - Z: rotation around the Z-axis (yaw/compass heading)
        /// - W: scalar/real part representing the magnitude of rotation
        /// 
        /// The quaternion is normalized (length = 1) and can be converted to:
        /// - Rotation matrix for 3D graphics
        /// - Euler angles (roll, pitch, yaw) for simple orientation
        /// - Axis-angle representation for animation interpolation
        /// </summary>
        public event EventHandler<OrientationSensorChangedEventArgs>? ReadingChanged;

        /// <summary>
        /// Exception message to display when attempting to start an already monitoring sensor.
        /// Used by the base AvaloniaSensor class for consistent error messaging.
        /// </summary>
        protected override string MonitoringExceptionMessage => "Orientation sensor has already been started.";

        /// <summary>
        /// Raises the ReadingChanged event with the provided orientation sensor data.
        /// This method is called by the platform-specific implementation when new
        /// orientation readings are received from the underlying platform APIs.
        /// </summary>
        /// <param name="reading">The orientation sensor data containing quaternion components
        /// (X, Y, Z, W) representing the device's absolute rotation in 3D space.
        /// The quaternion is normalized and provides a smooth, singularity-free
        /// representation of device orientation.</param>
        internal void RaiseReadingChanged(OrientationSensorData reading)
        {
            // Create event arguments with the orientation reading
            // Pass null as the sender because this method is typically called from:
            // 1. Static JSExport methods in WebAssembly (no instance context)
            // 2. Platform-specific code that doesn't have a meaningful sender reference
            ReadingChanged?.Invoke(null, new OrientationSensorChangedEventArgs(reading));
        }
    }
}