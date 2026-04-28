using Avalonia.Controls;
using Avalonia.Threading;
using Microsoft.Maui.Devices.Sensors;

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
            Accelerometer.ReadingChanged += (sender, args) =>
            {
                Dispatcher.UIThread.Post(() =>
                    AccelerometerLabel.Text = $"X:{args.Reading.Acceleration.X:F2} Y:{args.Reading.Acceleration.Y:F2} Z:{args.Reading.Acceleration.Z:F2}");
            };

            Accelerometer.Start(SensorSpeed.Default);
        }
    }

    void OnStopAccelerometer(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Accelerometer.IsSupported)
            Accelerometer.Stop();
    }
}