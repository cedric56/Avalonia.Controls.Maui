using Avalonia.Controls.Maui.Essentials;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;

namespace Avalonia.Controls.Maui.Tests.Services;

public class AvaloniaAccelerometerTests
{
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
    public void Start_ThrowsNotImplementedException()
    {
        var sensor = new AvaloniaAccelerometer();
        Assert.Throws<FeatureNotSupportedException>(() => sensor.Start(Microsoft.Maui.Devices.Sensors.SensorSpeed.Default));
    }

    [Fact]
    public void Stop_ThrowsNotImplementedException()
    {
        var sensor = new AvaloniaAccelerometer();
        Assert.Throws<FeatureNotSupportedException>(() => sensor.Stop());
    }
}
