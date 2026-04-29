using Avalonia.Controls;
using Avalonia.Threading;
using Microsoft.Maui.Devices.Sensors;
using System.Numerics.Colors;

namespace AvaloniaSandboxApp;

public partial class SensorsPage : ContentPage
{
    public SensorsPage()
    {
        InitializeComponent();

        AccelerometerLabel.Text = Accelerometer.IsSupported ? "Accelerometer: Supported" : "Accelerometer: No accelerometer detected";
    }

    // Accelerometer
    void OnStartAccelerometer(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Accelerometer.IsSupported)
        {
            Accelerometer.ReadingChanged += Accelerometer_ReadingChanged;
            Accelerometer.Start(SensorSpeed.Default);
        }
    }

    void OnStopAccelerometer(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Accelerometer.IsSupported)
        {
            Accelerometer.ReadingChanged -= Accelerometer_ReadingChanged;
            Accelerometer.Stop();
        }
    }

    private void Accelerometer_ReadingChanged(object? sender, AccelerometerChangedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
                    AccelerometerLabel.Text = $"X:{e.Reading.Acceleration.X:F2} Y:{e.Reading.Acceleration.Y:F2} Z:{e.Reading.Acceleration.Z:F2}");
    }

}