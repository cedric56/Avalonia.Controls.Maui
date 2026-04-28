using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;

namespace Avalonia.Controls.Maui.Essentials
{
    /// <summary>
    /// Abstract base class for all sensor implementations in the Avalonia MAUI Essentials library.
    /// Provides common sensor lifecycle management including start/stop operations, 
    /// state tracking, and error handling. Platform-specific sensor implementations
    /// inherit from this class and override the abstract methods.
    /// </summary>
    /// <remarks>
    /// This class implements the template method pattern for sensor operations:
    /// - Start() validates state, sets monitoring flag, then calls PlatformStart()
    /// - Stop() validates state, clears monitoring flag, then calls PlatformStop()
    /// - Error handling ensures state consistency even when exceptions occur
    /// </remarks>
    public abstract class AvaloniaSensor
    {
        /// <summary>
        /// Gets the exception message to display when attempting to start an already monitoring sensor.
        /// Each derived sensor provides its own specific message (e.g., "Accelerometer has already been started").
        /// </summary>
        protected abstract string MonitoringExceptionMessage { get; }

        /// <summary>
        /// Gets a value indicating whether the sensor is supported on the current platform.
        /// This should be overridden by derived classes to return true only when the
        /// underlying hardware and platform APIs are available.
        /// </summary>
        public abstract bool IsSupported { get; }

        /// <summary>
        /// Gets a value indicating whether the sensor is actively being monitored.
        /// True when Start() has been called successfully and sensor data is being collected.
        /// False when the sensor is stopped or has not been started yet.
        /// </summary>
        public bool IsMonitoring { get; protected set; }

        /// <summary>
        /// Start monitoring for changes to the sensor.
        /// This method validates the sensor state, sets the monitoring flag,
        /// and delegates to the platform-specific implementation.
        /// </summary>
        /// <remarks>
        /// Will throw <see cref="FeatureNotSupportedException"/> if not supported on device.
        /// Will throw <see cref="InvalidOperationException"/> if <see cref="IsMonitoring"/> is <see langword="true"/>.
        /// </remarks>
        /// <param name="sensorSpeed">The speed/precision to listen for changes. Higher speeds provide
        /// more frequent updates but consume more battery power.</param>
        public virtual void Start(SensorSpeed sensorSpeed)
        {
            // Validate that the sensor is supported on this platform
            if (!IsSupported)
                throw new FeatureNotSupportedException();

            // Prevent starting if already monitoring to avoid duplicate sensor connections
            if (IsMonitoring)
                throw new InvalidOperationException(MonitoringExceptionMessage);

            // Set monitoring flag before platform initialization to indicate we're attempting to start
            IsMonitoring = true;

            try
            {
                // Delegate to platform-specific implementation for actual sensor initialization
                PlatformStart(sensorSpeed);
            }
            catch
            {
                // Roll back monitoring flag if platform initialization fails
                // This ensures the object state remains consistent
                IsMonitoring = false;
                throw; // Re-throw the exception so the caller knows initialization failed
            }
        }

        /// <summary>
        /// Platform-specific implementation to start the sensor.
        /// This method must be overridden by derived classes to provide actual sensor
        /// initialization logic for each platform (Windows, Linux, WebAssembly, etc.).
        /// </summary>
        /// <param name="sensorSpeed">Desired sensor update speed/precision</param>
        protected abstract void PlatformStart(SensorSpeed sensorSpeed);

        /// <summary>
        /// Platform-specific implementation to stop the sensor.
        /// This method must be overridden by derived classes to clean up sensor resources
        /// and stop data collection for each platform.
        /// </summary>
        protected abstract void PlatformStop();

        /// <summary>
        /// Stop monitoring for changes to the sensor.
        /// This method validates the sensor state, clears the monitoring flag,
        /// and delegates to the platform-specific implementation.
        /// </summary>
        /// <remarks>
        /// Will throw <see cref="FeatureNotSupportedException"/> if not supported on device.
        /// If the sensor is not currently monitoring, this method returns immediately without action.
        /// </remarks>
        public void Stop()
        {
            // Validate that the sensor is supported on this platform
            if (!IsSupported)
                throw new FeatureNotSupportedException();

            // Return early if not currently monitoring (already stopped)
            // This makes Stop() idempotent - calling it multiple times is safe
            if (!IsMonitoring)
                return;

            // Clear monitoring flag before stopping platform-specific implementation
            IsMonitoring = false;

            try
            {
                // Delegate to platform-specific implementation for actual sensor cleanup
                PlatformStop();
            }
            catch
            {
                // Restore monitoring flag if platform stop fails (keep state consistent)
                // This ensures the object correctly reflects that monitoring is still active
                IsMonitoring = true;
                throw; // Re-throw the exception so the caller knows cleanup failed
            }
        }
    }
}