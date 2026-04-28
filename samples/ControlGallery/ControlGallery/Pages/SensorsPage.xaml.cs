namespace ControlGallery.Pages;

public partial class SensorsPage : ContentPage
{
	public SensorsPage()
	{
		InitializeComponent();
        AccelerometerLabel.Text = Accelerometer.IsSupported ? "Accelerometer: Supported" : "Accelerometer: No accelerometer detected";
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
}