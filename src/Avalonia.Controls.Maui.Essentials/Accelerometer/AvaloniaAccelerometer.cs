using Microsoft.Maui.Devices.Sensors;

namespace Avalonia.Controls.Maui.Essentials
{
    partial class AvaloniaAccelerometer : AccelerometerImplementation, IAccelerometer
    {
        private bool _isMonitoring;

        bool IAccelerometer.IsMonitoring => _isMonitoring;

        public new bool IsMonitoring => _isMonitoring;

        void IAccelerometer.Start(SensorSpeed sensorSpeed)
        {
            if (IsMonitoring)
                throw new InvalidOperationException("Accelerometer has already been started.");

            PlatformStart(sensorSpeed);
        }

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
