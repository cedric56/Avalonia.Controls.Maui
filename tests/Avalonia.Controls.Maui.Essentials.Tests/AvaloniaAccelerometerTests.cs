using Avalonia.Controls.Maui.Essentials;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;

namespace Avalonia.Controls.Maui.Tests.Services;

public class AvaloniaAccelerometerTests
{
    public AvaloniaAccelerometerTests()
    {
        var setDefaultMethod = typeof(Accelerometer).GetMethod("SetDefault",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static,
            [typeof(IAccelerometer)]);
        setDefaultMethod?.Invoke(null, [new AvaloniaAccelerometer()]);
    }

    [Fact]
    public void IsMonitoring_ReturnsFalse_WhenNotStarted()
    {
        var sensor = new AvaloniaAccelerometer();
        Assert.False(sensor.IsMonitoring);
    }

    [Fact]
    public void IsSupported_ReturnsFalse_WhenNotSupported()
    {
        var sensor = new AvaloniaAccelerometer();
        Assert.False(sensor.IsSupported);
    }

    [Fact]
    public void IsSupported_ReturnsFalse_OnAccelerometer()
    {      
        Assert.False(Accelerometer.IsSupported);
        Assert.False(Accelerometer.Default.IsSupported);
    }

    [Fact]
    public void IsMonitoring_ReturnsFalse_OnAccelerometer()
    {
        Assert.False(Accelerometer.IsMonitoring);
        Assert.False(Accelerometer.Default.IsMonitoring);
    }

    [Fact]
    public void Start_ThrowsNotImplementedException()
    {
        Assert.Throws<FeatureNotSupportedException>(() => Accelerometer.Start(Microsoft.Maui.Devices.Sensors.SensorSpeed.Default));
    }

    [Fact]
    public void Stop_ThrowsNotImplementedException()
    {
        Assert.Throws<FeatureNotSupportedException>(() => Accelerometer.Stop());
    }

    [Fact]
    public void IsAvaloniaAccelerometer()
    {
        Assert.True(Accelerometer.Default is AvaloniaAccelerometer);
    }
}
