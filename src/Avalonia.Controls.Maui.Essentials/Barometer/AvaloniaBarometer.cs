using Microsoft.Maui.Devices.Sensors;

namespace Avalonia.Controls.Maui.Essentials
{
    /// <summary>
    /// Platform-agnostic implementation of IBarometer for Avalonia environments.
    /// This partial class manages the barometer sensor lifecycle and event handling,
    /// while delegating platform-specific implementation to the partial class counterpart.
    /// The barometer measures atmospheric pressure in hectopascals (hPa), also known as millibars.
    /// </summary>
    public partial class AvaloniaBarometer : AvaloniaSensor, IBarometer
    {
        /// <summary>
        /// Exception message to display when attempting to start an already monitoring sensor.
        /// Used by the base AvaloniaSensor class for consistent error messaging.
        /// </summary>
        protected override string MonitoringExceptionMessage => "Barometer has already been started.";

        /// <summary>
        /// Occurs when the barometer reading changes.
        /// Provides atmospheric pressure data in hectopascals (hPa).
        /// The pressure reading represents the current atmospheric pressure at the device's location,
        /// which can be used for altitude estimation or weather trend prediction.
        /// </summary>
        public event EventHandler<BarometerChangedEventArgs>? ReadingChanged;

        /// <summary>
        /// Raises the ReadingChanged event with the provided barometer data.
        /// This method is called by the platform-specific implementation when new
        /// pressure readings are received from the underlying platform APIs.
        /// </summary>
        /// <param name="reading">The barometer data containing the atmospheric pressure
        /// measurement in hectopascals (hPa). Typical sea-level pressure is around 1013 hPa.
        /// Lower pressure often indicates approaching storms, higher pressure indicates fair weather.</param>
        protected void RaiseReadingChanged(BarometerData reading)
        {
            // Create event arguments with the barometer reading
            // Pass 'this' as the sender to identify which sensor instance raised the event
            ReadingChanged?.Invoke(this, new BarometerChangedEventArgs(reading));
        }
    }
}