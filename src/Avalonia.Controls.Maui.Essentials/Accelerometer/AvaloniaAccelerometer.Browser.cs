using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using System.Runtime.InteropServices.JavaScript;

namespace Avalonia.Controls.Maui.Essentials
{
    /// <summary>
    /// Platform-specific implementation of IAccelerometer for WebAssembly/Browser environments.
    /// This partial class provides the JavaScript interop layer that connects .NET accelerometer
    /// APIs to browser-based sensor APIs (DeviceMotionEvent).
    /// </summary>
    partial class AvaloniaAccelerometer : AccelerometerImplementation, IAccelerometer
    {
        bool PlatformIsSupported() => true;

        /// <summary>
        /// Starts listening to accelerometer data from the browser's DeviceMotionEvent API.
        /// This method is imported from the JavaScript module and calls the Web API to begin
        /// receiving accelerometer updates from the device's motion sensors.
        /// </summary>
        /// <param name="frequency">Desired update frequency in Hz (updates per second).
        /// Common values: 10-60 Hz depending on SensorSpeed setting.</param>
        [JSImport("accelerometerInterop.startListening", JSSensors.ModuleName)]
        public static partial void StartListening(int frequency);

        /// <summary>
        /// Stops listening to accelerometer data from the browser.
        /// This method is imported from the JavaScript module and disconnects the event handlers
        /// that were receiving device motion updates. It also resets the throttling state.
        /// </summary>
        [JSImport("accelerometerInterop.stopListening", JSSensors.ModuleName)]
        public static partial void StopListening();

        /// <summary>
        /// JavaScript-callable method that receives accelerometer data from the Web API.
        /// This method is exported to JavaScript and invoked by the browser's DeviceMotionEvent
        /// when acceleration data changes (subject to throttling based on requested frequency).
        /// </summary>
        /// <param name="x">Acceleration along the X-axis (left/right) in m/s², including gravity.
        /// Positive values indicate acceleration to the right.</param>
        /// <param name="y">Acceleration along the Y-axis (forward/back) in m/s², including gravity.
        /// Positive values indicate acceleration forward.</param>
        /// <param name="z">Acceleration along the Z-axis (up/down) in m/s², including gravity.
        /// Positive values indicate acceleration upward.</param>
        /// <remarks>
        /// The acceleration includes gravity, meaning when the device is stationary,
        /// the values represent the orientation of the device relative to Earth's gravity.
        /// For example, a device lying flat on a table would have Z ≈ 9.81, X ≈ 0, Y ≈ 0.
        /// </remarks>
        [JSExport]
        public static void OnReadingChanged(double x, double y, double z)
        {
            // Check if the default accelerometer instance is our WebAssembly implementation
            // and that it's currently monitoring before raising events
            if (Accelerometer.Default is AvaloniaAccelerometer implementation &&
                implementation.IsMonitoring)
            {
                // Create event args with the acceleration data (expressed in m/s²)
                // The data includes gravity, which is expected for the accelerometer API
                implementation.OnChanged(new AccelerometerChangedEventArgs(new AccelerometerData(x, y, z)));
            }
        }

        /// <summary>
        /// Platform-specific implementation to start the accelerometer.
        /// This method ensures the JavaScript module is loaded before attempting to start
        /// the sensor listeners.
        /// </summary>
        /// <param name="sensorSpeed">The desired speed/precision of sensor updates.
        /// This is converted to a frequency value (Hz) using the ToPlatform() extension method:
        /// - SensorSpeed.Fastest → ~60 Hz (maximum frequency)
        /// - SensorSpeed.Game → ~30 Hz (good for real-time)
        /// - SensorSpeed.UI → ~15 Hz (good for UI updates)
        /// - SensorSpeed.Default → ~10 Hz (balanced default)
        /// - SensorSpeed.Normal → ~5 Hz (power efficient)</param>
        async void PlatformStart(SensorSpeed sensorSpeed)
        {
            // Ensure the JavaScript sensor module is fully loaded and initialized
            // ConfigureAwait(false) prevents deadlocks when called from UI contexts
            await JSSensors.EnsureModuleLoadedAsync().ConfigureAwait(false);

            StartListening(GetFrequency(sensorSpeed));
        }

        /// <summary>
        /// Platform-specific implementation to stop the accelerometer.
        /// Simply calls the JavaScript function to remove event listeners and stop updates.
        /// </summary>
        void PlatformStop()
        {
            StopListening();

            _isMonitoring = false;
        }

        private int GetFrequency(SensorSpeed sensorSpeed)
        {
            switch (sensorSpeed)
            {
                case SensorSpeed.Default:
                    return 30;
                case SensorSpeed.UI:
                    return 15;
                case SensorSpeed.Game:
                    return 60;
                case SensorSpeed.Fastest:
                    return 100;
            }
            return 30;
        }
    }
}