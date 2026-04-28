using Microsoft.Maui.Devices.Sensors;

namespace Avalonia.Controls.Maui.Essentials
{
    /// <summary>
    /// Platform-agnostic implementation of IMagnetometer for Avalonia environments.
    /// This partial class manages the magnetometer sensor lifecycle and event handling,
    /// while delegating platform-specific implementation to the partial class counterpart.
    /// The magnetometer measures magnetic field strength in microteslas (μT) including the Earth's
    /// magnetic field and local magnetic interference.
    /// </summary>
    public partial class AvaloniaMagnetometer : AvaloniaSensor, IMagnetometer
    {
        /// <summary>
        /// Exception message to display when attempting to start an already monitoring sensor.
        /// Used by the base AvaloniaSensor class for consistent error messaging.
        /// </summary>
        protected override string MonitoringExceptionMessage => "Magnetometer has already been started.";

        /// <summary>
        /// Occurs when the magnetometer reading changes.
        /// Provides magnetic field strength data for X, Y, and Z axes in microteslas (μT).
        /// 
        /// Typical values:
        /// - Earth's magnetic field: 25-65 μT
        /// - Near speakers/magnets: 100-1000 μT
        /// - Medical MRI machines: up to 3,000,000 μT (3 T)
        /// 
        /// Applications include:
        /// - Compass heading calculation (using X and Y components)
        /// - Metal detection
        /// - Magnetic field monitoring
        /// - Device orientation detection in combination with accelerometer
        /// </summary>
        public event EventHandler<MagnetometerChangedEventArgs>? ReadingChanged;

        /// <summary>
        /// Raises the ReadingChanged event with the provided magnetometer data.
        /// This method is called by the platform-specific implementation when new
        /// magnetic field readings are received from the underlying platform APIs.
        /// </summary>
        /// <param name="data">The magnetometer data containing magnetic field strength
        /// measurements for X, Y, and Z axes in microteslas (μT).</param>
        protected void RaiseReadingChanged(MagnetometerData data)
        {
            // Create event arguments with the magnetometer reading
            // Pass 'this' as the sender to identify which sensor instance raised the event
            ReadingChanged?.Invoke(this, new MagnetometerChangedEventArgs(data));
        }
    }
}