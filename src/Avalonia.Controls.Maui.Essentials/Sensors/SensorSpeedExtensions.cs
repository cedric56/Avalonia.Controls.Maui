using Microsoft.Maui.Devices.Sensors;

namespace Avalonia.Controls.Maui.Essentials
{
    internal static partial class SensorSpeedExtensions
    {
        // Timing intervals to match Android sensor speeds in milliseconds
        // https://developer.android.com/guide/topics/sensors/sensors_overview
        internal const uint sensorIntervalDefault = 200;
        internal const uint sensorIntervalUI = 60;
        internal const uint sensorIntervalGame = 20;
        internal const uint sensorIntervalFastest = 5;

        internal static uint ToPlatform(this SensorSpeed sensorSpeed)
        {
            switch (sensorSpeed)
            {
                case SensorSpeed.Fastest:
                    return sensorIntervalFastest;
                case SensorSpeed.Game:
                    return sensorIntervalGame;
                case SensorSpeed.UI:
                    return sensorIntervalUI;
            }

            return sensorIntervalDefault;
        }
    }
}
