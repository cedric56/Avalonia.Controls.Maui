using Microsoft.Maui.Devices.Sensors;
using System.Runtime.InteropServices.JavaScript;

namespace Avalonia.Controls.Maui.Essentials
{
    /// <summary>
    /// Platform-specific implementation of IGyroscope for WebAssembly/Browser environments.
    /// This partial class provides the JavaScript interop layer that connects .NET gyroscope APIs
    /// to browser-based sensor APIs (DeviceMotionEvent or Generic Sensor API).
    /// The gyroscope measures angular velocity (rotation rate) in radians per second (rad/s)
    /// around the device's three axes (X: pitch, Y: roll, Z: yaw).
    /// </summary>
    partial class AvaloniaGyroscope
    {
        /// <summary>
        /// Indicates whether the gyroscope is supported in the current browser environment.
        /// For WebAssembly, this always returns true as support detection is handled
        /// by the JavaScript interop layer checking for Gyroscope API or DeviceMotionEvent availability.
        /// </summary>
        public override bool IsSupported => true;

        /// <summary>
        /// Starts listening to gyroscope/rotation rate data from the browser.
        /// This method is imported from the JavaScript module and begins monitoring
        /// device rotation rates around all three axes.
        /// </summary>
        /// <param name="frequency">Desired update frequency in Hz (updates per second).
        /// Common values: 15-60 Hz depending on SensorSpeed setting.</param>
        [JSImport("gyroscopeInterop.startListening", JSSensors.ModuleName)]
        public static partial void StartListening(int frequency);  // Note: Parameter name 'frequency' (fixed from 'frequancy')

        /// <summary>
        /// Stops listening to gyroscope/rotation rate data from the browser.
        /// This method is imported from the JavaScript module and disconnects the
        /// motion event handlers or Generic Sensor API connections.
        /// </summary>
        [JSImport("gyroscopeInterop.stopListening", JSSensors.ModuleName)]
        public static partial void StopListening();

        /// <summary>
        /// JavaScript-callable method that receives gyroscope/rotation rate data from the Web API.
        /// This method is exported to JavaScript and invoked by the browser when rotation data changes.
        /// </summary>
        /// <param name="x">Angular velocity around the X-axis (radians/second) - corresponds to beta/roll rotation.
        /// Positive values indicate rotation to the right.</param>
        /// <param name="y">Angular velocity around the Y-axis (radians/second) - corresponds to gamma/pitch rotation.
        /// Positive values indicate rotation away from the user.</param>
        /// <param name="z">Angular velocity around the Z-axis (radians/second) - corresponds to alpha/yaw rotation.
        /// Positive values indicate clockwise rotation.</param>
        /// <remarks>
        /// Units are in radians per second (rad/s). To convert to degrees per second, multiply by (180/π) ≈ 57.2958.
        /// Typical maximum rotation rates for devices range from 10-100 rad/s depending on the sensor.
        /// </remarks>
        [JSExport]
        public static void OnReadingChanged(double x, double y, double z)
        {
            // Check if the default gyroscope instance is our WebAssembly implementation
            // and that it's currently monitoring before raising events
            if (Gyroscope.Default is AvaloniaGyroscope implementation &&
                implementation.IsMonitoring)
            {
                // Raise the ReadingChanged event with the rotation rate data
                // Pass null as sender since this is a static method called from JavaScript
                implementation.RaiseReadingChanged(new GyroscopeData(x, y, z));
            }
        }

        /// <summary>
        /// Platform-specific implementation to start the gyroscope.
        /// Ensures the JavaScript module is loaded before starting sensor listeners.
        /// The gyroscope measures angular velocity (rotation speed) in radians per second.
        /// </summary>
        /// <param name="sensorSpeed">The desired speed/precision of sensor updates.
        /// This is converted to a frequency value (Hz) using the ToPlatform() extension method:
        /// - SensorSpeed.Fastest → ~60 Hz (maximum frequency)
        /// - SensorSpeed.Game → ~30 Hz (good for real-time)
        /// - SensorSpeed.UI → ~15 Hz (good for UI updates)
        /// - SensorSpeed.Default → ~10 Hz (balanced default)
        /// - SensorSpeed.Normal → ~5 Hz (power efficient)</param>
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
        /// Platform-specific implementation to stop the gyroscope.
        /// Calls the JavaScript function to remove event listeners, stop updates,
        /// and updates the monitoring state.
        /// </summary>
        protected override void PlatformStop()
        {
            // Stop JavaScript sensor listeners (DeviceMotionEvent or Generic Sensor API)
            StopListening();

            // Mark monitoring as inactive to prevent data forwarding
            IsMonitoring = false;
        }
    }
}