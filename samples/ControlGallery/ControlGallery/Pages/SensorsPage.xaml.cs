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
            Accelerometer.ReadingChanged += Accelerometer_ReadingChanged;
            Accelerometer.Start(SensorSpeed.Default);
        }
    }

    private void Accelerometer_ReadingChanged(object? sender, AccelerometerChangedEventArgs e)
    {
        AccelerometerLabel.Text = $"X:{e.Reading.Acceleration.X:F2} Y:{e.Reading.Acceleration.Y:F2} Z:{e.Reading.Acceleration.Z:F2}";
    }

    void OnAccelerometerStop(object? sender, EventArgs e)
    {
        if (Accelerometer.IsSupported)
        {
            Accelerometer.ReadingChanged -= Accelerometer_ReadingChanged;
            Accelerometer.Stop();
        }
    }
}