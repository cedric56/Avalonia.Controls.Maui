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
        GyroscopeLabel.Text = Gyroscope.IsSupported ? "Gyroscope: Supported" : "Gyroscope: No gyroscope detected";
        CompassLabel.Text = Compass.IsSupported ? "Compass: Supported" : "Compass: No compass detected";
        MagnetometerLabel.Text = Magnetometer.IsSupported ? "Magnetometer: Supported" : "Magnetometer: No magnetometer detected";
        BarometerLabel.Text = Barometer.IsSupported ? "Barometer: Supported" : "Barometer: No barometer detected";
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

    void OnStartBarometer(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Barometer.IsSupported)
        {
            Barometer.ReadingChanged += (sender, args) =>
            {
                Dispatcher.UIThread.Post(() =>
                    BarometerLabel.Text = $"Pressure: {args.Reading.PressureInHectopascals:hPa}");
            };

            Barometer.Start(SensorSpeed.Default);
        }
    }

    void OnStopBarometer(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Barometer.IsSupported)
            Barometer.Stop();
    }

    void OnStartMagnetometer(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Magnetometer.IsSupported)
        {
            Magnetometer.ReadingChanged += (sender, args) =>
            {
                Dispatcher.UIThread.Post(() =>
                    MagnetometerLabel.Text = $"X: {args.Reading.MagneticField.X:F2} Y: {args.Reading.MagneticField.Y:F2} Z: {args.Reading.MagneticField.Z:F2}");
            };

            Magnetometer.Start(SensorSpeed.Default);
        }
    }

    void OnStopMagnetometer(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Magnetometer.IsSupported)
            Magnetometer.Stop();
    }


    // Gyroscope
    void OnStartGyroscope(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Gyroscope.IsSupported)
        {
            Gyroscope.ReadingChanged += (sender, args) =>
            {
                var data = args.Reading;
                Dispatcher.UIThread.Post(() =>
                    GyroscopeLabel.Text = $"X:{data.AngularVelocity.X:F2} Y:{data.AngularVelocity.Y:F2} Z:{data.AngularVelocity.Z:F2}");
            };
            Gyroscope.Start(SensorSpeed.Default);
        }
    }

    void OnStopGyroscope(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Gyroscope.IsSupported)
            Gyroscope.Stop();
    }

    // Compass
    void OnStartCompass(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Compass.IsSupported)
        {
            Compass.ReadingChanged += (sender, args) =>
            {
                var heading = args.Reading.HeadingMagneticNorth;
                Dispatcher.UIThread.Post(() =>
                    CompassLabel.Text = $"Heading: {heading:F2}");
            };
            Compass.Start(SensorSpeed.Default);
        }
    }

    void OnStopCompass(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Compass.IsSupported)
            Compass.Stop();
    }
}