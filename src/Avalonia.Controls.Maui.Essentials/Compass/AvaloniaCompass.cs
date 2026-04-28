using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;

namespace Avalonia.Controls.Maui.Essentials
{
    /// <summary>
    /// Platform-agnostic implementation of ICompass for Avalonia environments.
    /// This partial class manages the compass sensor lifecycle, event handling, and state management,
    /// while delegating platform-specific implementation to the partial class counterpart.
    /// The compass provides heading/direction relative to magnetic north, measured in degrees (0-360°).
    /// </summary>
    public partial class AvaloniaCompass : AvaloniaSensor, ICompass
    {
        /// <summary>
        /// Exception message to display when attempting to start an already monitoring sensor.
        /// Used by the base AvaloniaSensor class for consistent error messaging.
        /// </summary>
        protected override string MonitoringExceptionMessage => "Compass has already been started.";

        /// <summary>
        /// Occurs when the compass reading changes.
        /// Provides the current heading in degrees, where 0° represents magnetic north,
        /// 90° represents east, 180° represents south, and 270° represents west.
        /// </summary>
        public event EventHandler<CompassChangedEventArgs>? ReadingChanged;

        /// <summary>
        /// Platform-specific implementation to start the compass with default filtering.
        /// This overload calls the main Start method with low-pass filtering enabled by default.
        /// </summary>
        /// <param name="sensorSpeed">The desired speed/precision of sensor updates</param>
        protected override void PlatformStart(SensorSpeed sensorSpeed) =>
            Start(sensorSpeed, true);

        /// <summary>
        /// Starts monitoring for changes to the device's heading/direction with optional filtering.
        /// This method initializes the platform-specific sensor monitoring and begins
        /// raising ReadingChanged events when compass data is available.
        /// </summary>
        /// <param name="sensorSpeed">The speed/precision to listen for changes. Higher speeds provide more frequent
        /// updates but consume more battery power.</param>
        /// <param name="applyLowPassFilter">Whether to apply a low-pass filter to smooth the heading data.
        /// True = filtered/smoothed readings (recommended for most apps, reduces jitter),
        /// False = raw/unfiltered readings (more responsive but potentially noisier).</param>
        /// <exception cref="FeatureNotSupportedException">Thrown when compass is not supported on the device.</exception>
        /// <exception cref="InvalidOperationException">Thrown when already monitoring (Start called twice without Stop).</exception>
        public void Start(SensorSpeed sensorSpeed, bool applyLowPassFilter)
        {
            // Validate that the compass is supported on this platform
            if (!IsSupported)
                throw new FeatureNotSupportedException();

            // Prevent starting if already monitoring to avoid duplicate sensor connections
            if (IsMonitoring)
                throw new InvalidOperationException(MonitoringExceptionMessage);

            // Set monitoring flag before platform initialization
            IsMonitoring = true;

            try
            {
                // Delegate to platform-specific implementation (WebAssembly JavaScript interop)
                // Pass both sensor speed and low-pass filter preference to the platform
                PlatformStart(sensorSpeed, applyLowPassFilter);
            }
            catch
            {
                // Roll back monitoring flag if platform initialization fails
                IsMonitoring = false;
                throw;
            }
        }

        /// <summary>
        /// Raises the ReadingChanged event with the provided compass data.
        /// This method is called by the platform-specific implementation when new
        /// heading readings are received from the underlying platform APIs.
        /// </summary>
        /// <param name="data">The compass data containing the current heading in degrees (0-360°).
        /// The heading represents the direction the device is pointing relative to magnetic north.</param>
        internal void RaiseReadingChanged(CompassData data)
        {
            // Create event arguments with the compass reading
            // Pass null as sender because this is typically called from the static
            // JSExport method OnReadingChanged in the platform-specific implementation,
            // not from an instance context
            ReadingChanged?.Invoke(null, new CompassChangedEventArgs(data));
        }
    }
}