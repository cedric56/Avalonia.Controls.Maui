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
            PlatformStart(sensorSpeed);
        }
        void IAccelerometer.Stop()
        {
            PlatformStop();
        }
    }
}
