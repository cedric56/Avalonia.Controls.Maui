using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;

namespace Avalonia.Controls.Maui.Essentials
{
    partial class AvaloniaAccelerometer : AccelerometerImplementation, IAccelerometer
    {
        private bool _isMonitoring;

        /// <summary>
        /// Indicates whether the accelerometer is currently monitoring sensor readings.
        /// Implementation of IAccelerometer.IsMonitoring property.
        /// </summary>
        bool IAccelerometer.IsMonitoring => _isMonitoring;

        bool IAccelerometer.IsSupported => IsSupported;

        /// <summary>
        /// Indicates whether the accelerometer is currently monitoring sensor readings.
        /// Public version that hides the base implementation (using 'new' keyword).
        /// </summary>
        public new bool IsMonitoring => _isMonitoring;

        /// <summary>
        /// Indicates whether the accelerometer is currently supportd sensor.
        /// Public version that hides the base implementation (using 'new' keyword).
        /// Returning value define by platform-specific implementation of PlatformIsSupported property.
        /// </summary>
        public new bool IsSupported => PlatformIsSupported();

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
        void IAccelerometer.Start(SensorSpeed sensorSpeed)
        {
            if (!IsSupported)
                throw new FeatureNotSupportedException();

            if (IsMonitoring)
                throw new InvalidOperationException("Accelerometer has already been started.");

            _isMonitoring = true;

            PlatformStart(sensorSpeed);
        }

        /// <summary>
        /// Stops monitoring accelerometer readings and releases sensor resources.
        /// </summary>
        /// <remarks>
        /// This method is safe to call even if the accelerometer isn't currently monitoring.
        /// PlatformStop() may throw exceptions (e.g., if sensor hardware fails), but we ensure
        /// _isMonitoring is reset in a finally block to maintain a consistent state.
        /// </remarks>
        void IAccelerometer.Stop()
        {
            if (!IsMonitoring)
                return;

            try
            {
                PlatformStop();
            }
            finally
            {
                _isMonitoring = false; // Ensure we reset the monitoring state even if stopping fails
            }
        }
    }
}
