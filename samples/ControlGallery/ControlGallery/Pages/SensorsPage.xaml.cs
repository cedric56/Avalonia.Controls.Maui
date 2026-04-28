namespace ControlGallery.Pages;

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
        OrientationSensorLabel.Text = OrientationSensor.IsSupported ? "OrientationSensor: Supported" : "OrientationSensor: No orientation sensor detected";
    }

    // Accelerometer
    void OnAccelerometerStart(object? sender, EventArgs e)
    {
        if (Accelerometer.IsSupported)
        {
            Accelerometer.ReadingChanged += (sender, args) =>
            {
                //MainThread.BeginInvokeOnMainThread(() =>
                AccelerometerLabel.Text = $"X:{args.Reading.Acceleration.X:F2} Y:{args.Reading.Acceleration.Y:F2} Z:{args.Reading.Acceleration.Z:F2}";
                //);
            };

            Accelerometer.Start(SensorSpeed.Default);
        }
    }

    void OnAccelerometerStop(object? sender, EventArgs e)
    {
        if (Accelerometer.IsSupported)
            Accelerometer.Stop();
    }

    void OnBarometerStart(object? sender, EventArgs e)
    {
        if (Barometer.IsSupported)
        {
            Barometer.ReadingChanged += (sender, args) =>
            {
                //MainThread.BeginInvokeOnMainThread(() =>
                BarometerLabel.Text = $"Pressure: {args.Reading.PressureInHectopascals:hPa}";
                //);
            };

            Barometer.Start(SensorSpeed.Default);
        }
    }

    void OnBarometerStop(object? sender, EventArgs e)
    {
        if (Barometer.IsSupported)
            Barometer.Stop();
    }

    void OnMagnetometerStart(object? sender, EventArgs e)
    {
        if (Magnetometer.IsSupported)
        {
            Magnetometer.ReadingChanged += (sender, args) =>
            {
                //MainThread.BeginInvokeOnMainThread(() =>
                    MagnetometerLabel.Text = $"X: {args.Reading.MagneticField.X:F2} Y: {args.Reading.MagneticField.Y:F2} Z: {args.Reading.MagneticField.Z:F2}";
                //);
            };

            Magnetometer.Start(SensorSpeed.Default);
        }
    }

    void OnMagnetometerStop(object? sender, EventArgs e)
    {
        if (Magnetometer.IsSupported)
            Magnetometer.Stop();
    }


    // Gyroscope
    void OnGyroscopeStart(object? sender, EventArgs e)
    {
        if (Gyroscope.IsSupported)
        {
            Gyroscope.ReadingChanged += (sender, args) =>
            {
                var data = args.Reading;
                //MainThread.BeginInvokeOnMainThread(() =>
                GyroscopeLabel.Text = $"X:{data.AngularVelocity.X:F2} Y:{data.AngularVelocity.Y:F2} Z:{data.AngularVelocity.Z:F2}";
                //);
            };
            Gyroscope.Start(SensorSpeed.Default);
        }
    }

    void OnGyroscopeStop(object? sender, EventArgs e)
    {
        if (Gyroscope.IsSupported)
            Gyroscope.Stop();
    }

    // Compass
    void OnCompassStart(object? sender, EventArgs e)
    {
        if (Compass.IsSupported)
        {
            Compass.ReadingChanged += (sender, args) =>
            {
                var heading = args.Reading.HeadingMagneticNorth;
                //MainThread.BeginInvokeOnMainThread(() =>
                    CompassLabel.Text = $"Heading: {heading:F2}";
                //);
            };
            Compass.Start(SensorSpeed.Default);
        }
    }

    void OnCompassStop(object? sender, EventArgs e)
    {
        if (Compass.IsSupported)
            Compass.Stop();
    }

    void OnOrientationSensorStart(object? sender, EventArgs e)
    {
        if (OrientationSensor.IsSupported)
        {
            OrientationSensor.ReadingChanged += (sender, args) =>
            {
                var heading = args.Reading.Orientation;
                //MainThread.BeginInvokeOnMainThread(() =>
                    OrientationSensorLabel.Text = $"Orientation: {heading}";
                //);
            };
            OrientationSensor.Start(SensorSpeed.Default);
        }
    }

    void OnOrientationSensorStop(object? sender, EventArgs e)
    {
        if (OrientationSensor.IsSupported)
            OrientationSensor.Stop();
    }
}