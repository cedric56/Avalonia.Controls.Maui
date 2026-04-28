using Microsoft.Maui.Devices.Sensors;
using System.Runtime.InteropServices.JavaScript;

namespace Avalonia.Controls.Maui.Essentials
{
    /// <summary>
    /// Platform-specific implementation of IMagnetometer for WebAssembly/Browser environments.
    /// This partial class provides the JavaScript interop layer that connects .NET magnetometer APIs
    /// to browser-based Generic Sensor API (Magnetometer interface).
    /// The magnetometer measures magnetic field strength in microteslas (μT) along X, Y, and Z axes,
    /// including the Earth's magnetic field and local magnetic interference.
    /// </summary>
    partial class AvaloniaMagnetometer
    {
        /// <summary>
        /// Indicates whether the magnetometer is supported in the current browser environment.
        /// For WebAssembly, this always returns true as support detection is handled
        /// by the JavaScript interop layer checking for the availability of the
        /// Magnetometer API in the browser.
        /// </summary>
        public override bool IsSupported => true;

        /// <summary>
        /// Starts listening to magnetometer/magnetic field data from the browser.
        /// This method is imported from the JavaScript module and begins monitoring
        /// magnetic field strength along all three axes using the Generic Sensor API.
        /// </summary>
        /// <param name="frequency">Desired update frequency in Hz (updates per second).
        /// Common values: 10-60 Hz depending on SensorSpeed setting.</param>
        [JSImport("magnetometerInterop.startListening", JSSensors.ModuleName)]
        public static partial void StartListening(int frequency);

        /// <summary>
        /// Stops listening to magnetometer/magnetic field data from the browser.
        /// This method is imported from the JavaScript module and disconnects the
        /// Magnetometer API sensor connection and removes event handlers.
        /// </summary>
        [JSImport("magnetometerInterop.stopListening", JSSensors.ModuleName)]
        public static partial void StopListening();

        /// <summary>
        /// JavaScript-callable method that receives magnetometer/magnetic field data from the Web API.
        /// This method is exported to JavaScript and invoked by the browser's Magnetometer API
        /// when magnetic field readings change.
        /// </summary>
        /// <param name="x">Magnetic field strength along the X-axis in microteslas (μT) - left/right direction.
        /// Positive values typically indicate eastward direction.</param>
        /// <param name="y">Magnetic field strength along the Y-axis in microteslas (μT) - forward/back direction.
        /// Positive values typically indicate northward direction.</param>
        /// <param name="z">Magnetic field strength along the Z-axis in microteslas (μT) - up/down direction.
        /// Positive values typically indicate upward direction.</param>
        /// <remarks>
        /// The magnetometer measures the Earth's magnetic field along with any local magnetic interference.
        /// Values typically range from 25-65 μT for Earth's magnetic field, but can be higher near
        /// magnetic sources (speakers, motors, magnets).
        /// This data is commonly used for:
        /// - Compass heading calculations (using X and Y components)
        /// - Metal detection
        /// - Magnetic field strength monitoring
        /// </remarks>
        [JSExport]
        public static void OnReadingChanged(double x, double y, double z)
        {
            // Check if the default magnetometer instance is our WebAssembly implementation
            // and that it's currently monitoring before raising events
            if (Magnetometer.Default is AvaloniaMagnetometer implementation &&
                implementation.IsMonitoring)
            {
                // Raise the ReadingChanged event with the magnetic field data
                // Pass null as sender since this is a static method called from JavaScript
                implementation.RaiseReadingChanged(new MagnetometerData(x, y, z));
            }
        }

        /// <summary>
        /// Platform-specific implementation to start the magnetometer.
        /// Ensures the JavaScript module is loaded before starting sensor listeners.
        /// The magnetometer measures magnetic field strength including the Earth's magnetic field
        /// and local magnetic interference.
        /// </summary>
        /// <param name="sensorSpeed">The desired speed/precision of sensor updates.
        /// This is converted to a frequency value (Hz) using the ToPlatform() extension method:
        /// - SensorSpeed.Fastest → ~60 Hz (maximum frequency, highest power consumption)
        /// - SensorSpeed.Game → ~30 Hz (good for real-time interactive scenarios)
        /// - SensorSpeed.UI → ~15 Hz (sufficient for UI feedback)
        /// - SensorSpeed.Default → ~10 Hz (balanced default)
        /// - SensorSpeed.Normal → ~5 Hz (power efficient, good for background tasks)</param>
        async protected override void PlatformStart(SensorSpeed sensorSpeed)
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
        /// Platform-specific implementation to stop the magnetometer.
        /// Calls the JavaScript function to disconnect the sensor, remove event listeners,
        /// and updates the monitoring state.
        /// </summary>
        protected override void PlatformStop()
        {
            // Stop JavaScript Magnetometer API sensor
            StopListening();

            // Mark monitoring as inactive to prevent data forwarding
            IsMonitoring = false;
        }
    }
}