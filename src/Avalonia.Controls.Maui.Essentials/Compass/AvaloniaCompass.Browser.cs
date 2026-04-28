using Microsoft.Maui.Devices.Sensors;
using System.Runtime.InteropServices.JavaScript;

namespace Avalonia.Controls.Maui.Essentials
{
    /// <summary>
    /// Platform-specific implementation of ICompass for WebAssembly/Browser environments.
    /// This partial class provides the JavaScript interop layer that connects .NET compass APIs
    /// to browser-based orientation sensor APIs (DeviceOrientationEvent).
    /// The compass provides heading in degrees relative to magnetic north (0-360°).
    /// </summary>
    partial class AvaloniaCompass
    {
        /// <summary>
        /// Indicates whether the compass is supported in the current browser environment.
        /// For WebAssembly, this always returns true as support detection is handled
        /// by the JavaScript interop layer checking for DeviceOrientationEvent availability.
        /// </summary>
        public override bool IsSupported => true;

        /// <summary>
        /// Starts listening to compass/orientation data from the browser.
        /// This method is imported from the JavaScript module and begins monitoring
        /// device orientation to calculate heading/direction.
        /// </summary>
        /// <param name="frequency">Desired update frequency in Hz (updates per second).
        /// Common values: 5-60 Hz depending on SensorSpeed setting.</param>
        [JSImport("compassInterop.startListening", JSSensors.ModuleName)]
        public static partial void StartListening(int frequency);

        /// <summary>
        /// Stops listening to compass/orientation data from the browser.
        /// This method is imported from the JavaScript module and disconnects the
        /// deviceorientation event handlers.
        /// </summary>
        [JSImport("compassInterop.stopListening", JSSensors.ModuleName)]
        public static partial void StopListening();

        /// <summary>
        /// JavaScript-callable method that receives compass heading data from the Web API.
        /// This method is exported to JavaScript and invoked by the browser's deviceorientation
        /// events when the device's heading changes.
        /// </summary>
        /// <param name="heading">Current heading in degrees (0-360), where:
        /// - 0° represents magnetic north
        /// - 90° represents east
        /// - 180° represents south
        /// - 270° represents west</param>
        [JSExport]
        public static void OnReadingChanged(double heading)
        {
            // Check if the default compass instance is our WebAssembly implementation
            // and that it's currently monitoring before raising events
            if (Compass.Default is AvaloniaCompass implementation &&
                implementation.IsMonitoring)
            {
                // Raise the ReadingChanged event with the heading data
                // Pass null as sender since this is a static method called from JavaScript
                implementation.RaiseReadingChanged(new CompassData(heading));
            }
        }

        /// <summary>
        /// Platform-specific implementation to start the compass.
        /// Ensures the JavaScript module is loaded before starting sensor listeners.
        /// Note: applyLowPassFilter parameter is not directly supported in WebAssembly
        /// and is handled by the JavaScript interop layer if needed.
        /// </summary>
        /// <param name="sensorSpeed">The desired speed/precision of sensor updates.
        /// This is converted to a frequency value (Hz) using the ToPlatform() extension method:
        /// - SensorSpeed.Fastest → ~60 Hz (maximum frequency)
        /// - SensorSpeed.Game → ~30 Hz (good for real-time)
        /// - SensorSpeed.UI → ~15 Hz (good for UI updates)
        /// - SensorSpeed.Default → ~10 Hz (balanced default)
        /// - SensorSpeed.Normal → ~5 Hz (power efficient)</param>
        /// <param name="applyLowPassFilter">Whether to apply low-pass filtering to smooth heading data
        /// (ignored on WebAssembly as filtering is handled by browser implementation)</param>
        async void PlatformStart(SensorSpeed sensorSpeed, bool applyLowPassFilter)
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
        /// Platform-specific implementation to stop the compass.
        /// Calls the JavaScript function to remove event listeners, stop updates,
        /// and updates the monitoring state.
        /// </summary>
        protected override void PlatformStop()
        {
            // Stop JavaScript sensor listeners (removes deviceorientation handlers)
            StopListening();

            // Mark monitoring as inactive to prevent data forwarding
            IsMonitoring = false;
        }
    }
}