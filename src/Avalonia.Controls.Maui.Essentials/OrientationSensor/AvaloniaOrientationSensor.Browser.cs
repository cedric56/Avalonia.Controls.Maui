using Microsoft.Maui.Devices.Sensors;
using System.Runtime.InteropServices.JavaScript;

namespace Avalonia.Controls.Maui.Essentials
{
    /// <summary>
    /// Platform-specific implementation of IOrientationSensor for WebAssembly/Browser environments.
    /// This partial class provides the JavaScript interop layer that connects .NET orientation sensor APIs
    /// to browser-based AbsoluteOrientationSensor API (Generic Sensor API) with fallback to DeviceOrientationEvent.
    /// The orientation sensor provides absolute device rotation in 3D space using quaternion
    /// representation (W, X, Y, Z components) relative to Earth's coordinate system.
    /// </summary>
    partial class AvaloniaOrientationSensor
    {
        /// <summary>
        /// Indicates whether the orientation sensor is supported in the current browser environment.
        /// For WebAssembly, this always returns true as support detection is handled
        /// by the JavaScript interop layer checking for the availability of the
        /// AbsoluteOrientationSensor API or DeviceOrientationEvent in the browser.
        /// </summary>
        public override bool IsSupported => true;

        /// <summary>
        /// Starts listening to absolute orientation data from the browser.
        /// This method is imported from the JavaScript module and begins monitoring
        /// device orientation in 3D space using quaternion representation.
        /// </summary>
        /// <param name="frequency">Desired update frequency in Hz (updates per second).
        /// Common values: 10-60 Hz depending on SensorSpeed setting.
        /// Note: Actual frequency may vary based on browser and hardware capabilities.</param>
        [JSImport("orientationInterop.startListening", JSSensors.ModuleName)]
        public static partial void StartListening(int frequency);

        /// <summary>
        /// Stops listening to absolute orientation data from the browser.
        /// This method is imported from the JavaScript module and disconnects the
        /// AbsoluteOrientationSensor API connection or removes DeviceOrientationEvent handlers.
        /// </summary>
        [JSImport("orientationInterop.stopListening", JSSensors.ModuleName)]
        public static partial void StopListening();

        /// <summary>
        /// JavaScript-callable method that receives absolute orientation data from the Web API.
        /// This method is exported to JavaScript and invoked by the browser's AbsoluteOrientationSensor
        /// or DeviceOrientationEvent when device orientation changes.
        /// </summary>
        /// <param name="x">Quaternion X component - rotation around the X-axis (pitch/forward-back tilt).
        /// Represents the imaginary i component in quaternion math.</param>
        /// <param name="y">Quaternion Y component - rotation around the Y-axis (roll/left-right tilt).
        /// Represents the imaginary j component in quaternion math.</param>
        /// <param name="z">Quaternion Z component - rotation around the Z-axis (yaw/compass heading).
        /// Represents the imaginary k component in quaternion math.</param>
        /// <param name="w">Quaternion W component - the scalar/real part representing the cosine of half the rotation angle.
        /// The real component that completes the unit quaternion.</param>
        /// <remarks>
        /// Orientation is represented as a unit quaternion (normalized, x² + y² + z² + w² = 1) that represents 
        /// the device's rotation relative to the Earth's coordinate system (north, east, up).
        /// 
        /// Quaternion advantages over Euler angles:
        /// - No gimbal lock (singularities at certain angles)
        /// - Smooth interpolation (slerp) between orientations
        /// - Efficient composition of rotations
        /// - Numerically stable for 3D graphics
        /// 
        /// To convert to Euler angles (roll, pitch, yaw):
        /// - pitch = atan2(2*(w*x + y*z), 1 - 2*(x² + y²))
        /// - roll  = asin(2*(w*y - z*x))
        /// - yaw   = atan2(2*(w*z + x*y), 1 - 2*(y² + z²))
        /// </remarks>
        [JSExport]
        public static void OnReadingChanged(double x, double y, double z, double w)
        {
            // Check if the default orientation sensor instance is our WebAssembly implementation
            // and that it's currently monitoring before raising events
            if (OrientationSensor.Default is AvaloniaOrientationSensor implementation &&
                implementation.IsMonitoring)
            {
                // Raise the ReadingChanged event with the quaternion orientation data
                // Pass null as sender since this is a static method called from JavaScript
                implementation.RaiseReadingChanged(new OrientationSensorData(x, y, z, w));
            }
        }

        /// <summary>
        /// Platform-specific implementation to start the absolute orientation sensor.
        /// Ensures the JavaScript module is loaded before starting sensor listeners.
        /// The orientation sensor provides absolute device rotation in 3D space using
        /// quaternion representation, referenced to Earth's coordinate system.
        /// </summary>
        /// <param name="sensorSpeed">The desired speed/precision of sensor updates.
        /// This is converted to a frequency value (Hz) using the ToPlatform() extension method:
        /// - SensorSpeed.Fastest → ~60 Hz (maximum frequency, highest power consumption)
        /// - SensorSpeed.Game → ~30 Hz (good for real-time interactive scenarios)
        /// - SensorSpeed.UI → ~15 Hz (sufficient for UI feedback)
        /// - SensorSpeed.Default → ~10 Hz (balanced default)
        /// - SensorSpeed.Normal → ~5 Hz (power efficient, good for background tasks)</param>
        protected async override void PlatformStart(SensorSpeed sensorSpeed)
        {
            // Ensure the JavaScript sensor module is fully loaded and initialized
            // ConfigureAwait(false) prevents deadlocks when called from UI contexts
            await JSSensors.EnsureModuleLoadedAsync().ConfigureAwait(false);

            // Convert SensorSpeed enum to platform-specific frequency (Hz) and start listening
            // ToPlatform() maps sensor speed to appropriate Hz values for WebAssembly
            StartListening((int)sensorSpeed.ToPlatform());

            // Mark monitoring as active so data can be forwarded to subscribers
            IsMonitoring = true;
        }

        /// <summary>
        /// Platform-specific implementation to stop the orientation sensor.
        /// Calls the JavaScript function to disconnect the sensor, remove event listeners,
        /// and updates the monitoring state.
        /// </summary>
        protected override void PlatformStop()
        {
            // Stop JavaScript AbsoluteOrientationSensor or remove DeviceOrientationEvent handlers
            StopListening();

            // Mark monitoring as inactive to prevent data forwarding
            IsMonitoring = false;
        }
    }
}