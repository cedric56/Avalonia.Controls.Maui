using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using System.Runtime.Versioning;
using Windows.Devices.Sensors;
using Magnetometer = Microsoft.Maui.Devices.Sensors.Magnetometer;
using Sensor = Windows.Devices.Sensors.Compass;

namespace Avalonia.Controls.Maui.Essentials
{
    /// <summary>
    /// Base platform-agnostic implementation for compass (heading/direction sensor).
    /// Provides default behavior for unsupported platforms - throws feature not supported exceptions.
    /// Platform-specific implementations override the virtual methods to provide actual sensor functionality.
    /// The compass provides heading in degrees relative to magnetic north (0-360°).
    /// </summary>
    partial class AvaloniaCompass
    {
        /// <summary>
        /// Indicates whether the compass is supported on the current platform.
        /// Base implementation returns false, indicating no support.
        /// Platform-specific implementations override this to return actual support status.
        /// </summary>
        public override bool IsSupported => false;

        /// <summary>
        /// Platform-specific implementation to start compass monitoring.
        /// Base implementation throws FeatureNotSupportedException.
        /// </summary>
        /// <param name="sensorSpeed">Desired sensor update speed/precision</param>
        /// <param name="applyLowPassFilter">Whether to apply low-pass filtering to smooth heading data</param>
        protected virtual void PlatformStart(SensorSpeed sensorSpeed, bool applyLowPassFilter) =>
            throw new FeatureNotSupportedException();

        /// <summary>
        /// Platform-specific implementation to stop compass monitoring.
        /// Base implementation throws FeatureNotSupportedException.
        /// </summary>
        protected override void PlatformStop() =>
            throw new FeatureNotSupportedException();
    }

    /// <summary>
    /// Windows-specific implementation of compass using the UWP Windows.Devices.Sensors.Compass API.
    /// Supported on Windows 10 build 10240 and later.
    /// Provides heading in degrees relative to magnetic north.
    /// Note: ReportInterval values are optimized based on Windows sensor platform recommendations.
    /// </summary>
    [SupportedOSPlatform("windows10.0.10240")]
    class WindowsCompass : AvaloniaCompass
    {
        // Optimized report intervals from Windows documentation:
        // https://docs.microsoft.com/en-us/uwp/api/windows.devices.sensors.compass.reportinterval
        // These values balance power consumption with update frequency
        internal const uint FastestInterval = 8;   // ~125 Hz - maximum update rate
        internal const uint GameInterval = 22;     // ~45 Hz - good for interactive scenarios
        internal const uint NormalInterval = 33;   // ~30 Hz - standard reading rate (default)

        // Keep a reference to the sensor so we can clean up and stop the same instance
        Sensor? sensor;

        /// <summary>
        /// Gets the default compass sensor on the Windows device.
        /// Returns null if no compass is present (e.g., devices without magnetometer).
        /// </summary>
        static Sensor DefaultCompass =>
            Sensor.GetDefault();

        /// <summary>
        /// Indicates whether a compass is available on this Windows device.
        /// Returns true if the default sensor exists, false otherwise.
        /// </summary>
        public override bool IsSupported =>
            DefaultCompass != null;

        /// <summary>
        /// Starts the Windows compass with the specified sensor speed and filter settings.
        /// Configures the report interval based on the requested speed and minimum sensor capabilities.
        /// Note: applyLowPassFilter is not directly supported on Windows compass API and is ignored.
        /// </summary>
        /// <param name="sensorSpeed">Desired sensor update frequency/precision</param>
        /// <param name="applyLowPassFilter">Whether to apply low-pass filtering (ignored on Windows)</param>
        protected override void PlatformStart(SensorSpeed sensorSpeed, bool applyLowPassFilter)
        {
            // Get the default compass sensor
            sensor = DefaultCompass;

            // Map SensorSpeed enum to Windows report intervals
            var interval = NormalInterval;  // Default to 33ms (~30 Hz)
            switch (sensorSpeed)
            {
                case SensorSpeed.Fastest:
                    interval = FastestInterval;  // 8ms (~125 Hz) - maximum frequency
                    break;
                case SensorSpeed.Game:
                    interval = GameInterval;     // 22ms (~45 Hz) - good for real-time
                    break;
                    // SensorSpeed.Default and SensorSpeed.UI use NormalInterval
                    // SensorSpeed.Normal also uses NormalInterval
            }

            // Set the report interval - use the larger of requested interval or sensor's minimum
            // This respects both the app's desired update rate and the sensor's hardware limitations
            sensor.ReportInterval = sensor.MinimumReportInterval >= interval ? sensor.MinimumReportInterval : interval;

            // Subscribe to reading changed events
            sensor.ReadingChanged += CompassReportedInterval;
        }

        /// <summary>
        /// Event handler for Windows compass readings.
        /// Extracts the magnetic north heading and raises the ReadingChanged event.
        /// </summary>
        /// <param name="sender">The compass sensor object</param>
        /// <param name="e">Event args containing the compass reading</param>
        void CompassReportedInterval(object sender, CompassReadingChangedEventArgs e)
        {
            // HeadingMagneticNorth provides degrees relative to magnetic north (0-360°)
            var data = new CompassData(e.Reading.HeadingMagneticNorth);
            RaiseReadingChanged(data);
        }

        /// <summary>
        /// Stops the Windows compass.
        /// Unsubscribes from events and resets the report interval to default.
        /// </summary>
        protected override void PlatformStop()
        {
            if (sensor == null)
                return;

            // Unsubscribe from reading events
            sensor.ReadingChanged -= CompassReportedInterval;

            // Reset report interval to 0 (default, no minimum interval)
            sensor.ReportInterval = 0;

            // Release the sensor reference
            sensor = null;
        }
    }

    /// <summary>
    /// Linux-specific implementation of compass using the magnetometer sensor.
    /// Since Linux doesn't have a dedicated compass sensor API, this implementation
    /// calculates heading from magnetometer readings (X and Y components of Earth's magnetic field).
    /// Heading is computed using the arctangent function: heading = atan2(Y, X) converted to degrees.
    /// </summary>
    class LinuxCompass : AvaloniaCompass
    {
        /// <summary>
        /// Indicates whether a compass is supported on Linux.
        /// Returns true if the magnetometer is supported (since compass derives from magnetometer data).
        /// </summary>
        public override bool IsSupported => Magnetometer.IsSupported;

        /// <summary>
        /// Starts the Linux compass by leveraging the magnetometer sensor.
        /// Subscribes to magnetometer readings and converts magnetic field vectors to heading.
        /// </summary>
        /// <param name="sensorSpeed">Desired sensor update frequency/precision</param>
        /// <param name="applyLowPassFilter">Whether to apply low-pass filtering to smooth heading data.
        /// This parameter is passed to the magnetometer but note that heading-specific filtering
        /// may need additional implementation in the magnetometer handler.</param>
        protected override void PlatformStart(SensorSpeed sensorSpeed, bool applyLowPassFilter)
        {
            // Start the magnetometer sensor with the requested speed
            // Note: applyLowPassFilter is handled by the magnetometer if supported
            Magnetometer.Start(sensorSpeed);

            // Subscribe to magnetometer reading changed events
            Magnetometer.ReadingChanged += Magnetometer_ReadingChanged;
        }

        /// <summary>
        /// Handles magnetometer reading changes and computes the heading direction.
        /// Converts magnetic field vector (X, Y components) to a heading angle in degrees.
        /// 
        /// The algorithm:
        /// 1. Extract X (east-west) and Y (north-south) magnetic field components
        /// 2. Calculate heading using atan2(Y, X) which returns angle in radians
        /// 3. Convert radians to degrees (multiply by 180/π)
        /// 4. Normalize negative headings to 0-360° range
        /// </summary>
        /// <param name="sender">The magnetometer sensor (unused)</param>
        /// <param name="e">Event args containing the magnetic field readings</param>
        private void Magnetometer_ReadingChanged(object? sender, MagnetometerChangedEventArgs e)
        {
            // Extract X and Y components of the Earth's magnetic field
            // X: magnetic field strength along east-west axis
            // Y: magnetic field strength along north-south axis
            var x = e.Reading.MagneticField.X;
            var y = e.Reading.MagneticField.Y;

            // Calculate heading using arctangent of Y/X
            // Math.Atan2 returns angle in radians between -π and π
            // Convert to degrees: multiply by (180/π)
            double heading = Math.Atan2(y, x) * (180.0 / Math.PI);

            // Normalize heading to 0-360° range
            // Atan2 returns negative values for angles > 180°, so add 360 to convert
            if (heading < 0) heading += 360;

            // Raise the compass reading changed event with calculated heading
            RaiseReadingChanged(new CompassData(heading));
        }

        /// <summary>
        /// Stops the Linux compass.
        /// Unsubscribes from magnetometer events and stops the magnetometer sensor.
        /// </summary>
        protected override void PlatformStop()
        {
            // Unsubscribe from magnetometer reading events
            Magnetometer.ReadingChanged -= Magnetometer_ReadingChanged;

            // Stop the magnetometer sensor
            Magnetometer.Stop();
        }
    }
}