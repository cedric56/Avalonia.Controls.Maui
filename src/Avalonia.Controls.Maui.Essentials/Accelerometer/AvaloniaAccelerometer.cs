using Microsoft.Maui.Devices.Sensors;
using System.Numerics;

namespace Avalonia.Controls.Maui.Essentials
{
    /// <summary>
    /// Accelerometer data of the acceleration of the device in three dimensional space.
    /// This class provides acceleration readings in m/s² and shake detection functionality.
    /// Inherits from AvaloniaSensor for common sensor lifecycle management.
    /// </summary>
    public partial class AvaloniaAccelerometer : AvaloniaSensor, IAccelerometer
    {
        /// <summary>
        /// Exception message to display when attempting to start an already monitoring sensor.
        /// Used by the base AvaloniaSensor class for consistent error messaging.
        /// </summary>
        protected override string MonitoringExceptionMessage => "Accelerometer has already been started.";

        /// <summary>
        /// Threshold value for shake detection (squared magnitude of acceleration).
        /// Value of 169 corresponds to approximately 13 m/s² when multiplied by gravity (13² = 169).
        /// This represents a significant acceleration event typical of a shake gesture.
        /// </summary>
        const double accelerationThreshold = 169;

        /// <summary>
        /// Standard gravity constant (9.81 m/s²) used to convert normalized acceleration values
        /// (in G-forces) to actual acceleration in meters per second squared.
        /// </summary>
        const double gravity = 9.81;

        /// <summary>
        /// Queue that tracks recent acceleration events to detect shake patterns.
        /// Maintains a sliding window of timestamped acceleration readings to determine
        /// if a shake gesture has occurred based on threshold crossing frequency.
        /// </summary>
        static readonly AccelerometerQueue queue = new AccelerometerQueue();

        /// <inheritdoc/>
        /// <summary>
        /// Occurs when accelerometer readings change.
        /// Provides acceleration data for X, Y, and Z axes in meters per second squared (m/s²).
        /// </summary>
        public event EventHandler<AccelerometerChangedEventArgs>? ReadingChanged;

        /// <inheritdoc/>
        /// <summary>
        /// Occurs when a shake gesture is detected on the device.
        /// This event is triggered when multiple acceleration spikes occur in quick succession,
        /// indicating the user is shaking the device.
        /// </summary>
        public event EventHandler? ShakeDetected;

        /// <summary>
        /// Handles accelerometer reading changes from the platform implementation.
        /// Invokes the ReadingChanged event and processes shake detection.
        /// </summary>
        /// <param name="e">Event args containing the accelerometer reading data including
        /// acceleration vector in G-forces (normalized to gravity).</param>
        internal void OnChanged(AccelerometerChangedEventArgs e)
        {
            // Raise the ReadingChanged event for all subscribers
            // Pass null as sender since this is an internal method called from platform code
            ReadingChanged?.Invoke(null, e);

            // Process shake detection only if there are subscribers to the event
            // This avoids unnecessary processing when no one is listening for shakes
            if (ShakeDetected != null)
                ProcessShakeEvent(e.Reading.Acceleration);
        }

        /// <summary>
        /// Processes acceleration data to detect shake gestures.
        /// Uses a time-based queue to track recent acceleration events and determines
        /// if the device is being shaken based on threshold crossing frequency.
        /// 
        /// The algorithm works by:
        /// 1. Converting normalized acceleration (in G's) to actual m/s²
        /// 2. Calculating the squared magnitude of the acceleration vector
        /// 3. Adding timestamps to a queue when acceleration exceeds threshold
        /// 4. Checking if enough events occurred recently to qualify as a shake
        /// 5. Raising ShakeDetected event when shake pattern is confirmed
        /// </summary>
        /// <param name="acceleration">Current acceleration vector in G-forces (normalized to gravity).
        /// Values represent multiples of Earth's gravity (1.0 = 9.81 m/s²).</param>
        void ProcessShakeEvent(Vector3 acceleration)
        {
            // Get current time in nanoseconds for precise interval calculations
            var now = Nanoseconds(DateTime.UtcNow);

            // Convert normalized acceleration from G-forces to actual acceleration in m/s²
            // Multiply by gravity (9.81) to convert from G-force to physical units
            var x = acceleration.X * gravity;
            var y = acceleration.Y * gravity;
            var z = acceleration.Z * gravity;

            // Calculate squared magnitude of acceleration vector (avoiding costly square root)
            // Comparing squared values with squared threshold is more efficient
            var g = x * x + y * y + z * z;

            // Add current timestamp and whether acceleration exceeds threshold to queue
            // The queue maintains a sliding window of recent acceleration events
            queue.Add(now, g > accelerationThreshold);

            // Check if the recent acceleration pattern indicates a shake gesture
            if (queue.IsShaking)
            {
                // Clear the queue to prevent immediate re-triggering of shake event
                // This ensures each shake gesture triggers only one event
                queue.Clear();

                // Create empty event args (no additional data needed for shake event)
                var args = new EventArgs();

                // Raise the ShakeDetected event for all subscribers
                // Pass null as sender since this is an internal method
                ShakeDetected?.Invoke(null, args);
            }
        }
        /// <summary>
        /// Converts a DateTime to nanoseconds since Unix epoch.
        /// Used for precise timing calculations in shake detection to accurately
        /// measure time intervals between acceleration spikes.
        /// </summary>
        /// <param name="time">The DateTime to convert to nanoseconds</param>
        /// <returns>Total nanoseconds since the Unix epoch (1970-01-01)</returns>
        static long Nanoseconds(DateTime time) =>
            (time.Ticks / TimeSpan.TicksPerMillisecond) * 1_000_000;
    }
}