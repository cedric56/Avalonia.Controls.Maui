using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;

namespace Avalonia.Controls.Maui.Essentials
{
    /// <summary>
    /// Partial class implementation of the barometer for platforms that do not support
    /// atmospheric pressure sensing. This class provides fallback behavior when no
    /// barometer hardware or platform API is available.
    /// </summary>
    partial class AvaloniaBarometer
    {
        /// <summary>
        /// Gets a value indicating whether the barometer is supported on the current platform.
        /// This default implementation returns false, indicating that no barometer hardware
        /// or platform API is available. Platform-specific implementations override this
        /// property to return true when a physical barometer sensor is present.
        /// </summary>
        /// <returns>Always returns false for the base implementation.</returns>
        public override bool IsSupported => false;

        /// <summary>
        /// Platform-specific implementation to start barometer monitoring.
        /// This base implementation throws a FeatureNotSupportedException because the
        /// current platform either lacks barometer hardware or the required platform APIs.
        /// </summary>
        /// <exception cref="FeatureNotSupportedException">Thrown when attempting to stop
        /// the barometer on an unsupported platform. This is always thrown by the base
        /// implementation.</exception>
        protected override void PlatformStart(SensorSpeed sensorSpeed)
        {
            throw new FeatureNotSupportedException();
        }

        /// <summary>
        /// Platform-specific implementation to stop barometer monitoring.
        /// This base implementation throws a FeatureNotSupportedException because the
        /// current platform doesn't support barometer functionality.
        /// </summary>
        /// <exception cref="FeatureNotSupportedException">Thrown when attempting to stop
        /// the barometer on an unsupported platform. This is always thrown by the base
        /// implementation.</exception>
        protected override void PlatformStop()
        {
            throw new FeatureNotSupportedException();
        }
    }
}