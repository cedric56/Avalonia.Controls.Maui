using Avalonia.Controls.Maui.Essentials;

namespace Avalonia.Controls.Maui.Tests.Services;

public class AvaloniaAccelerometerTests
{
    [Fact]
    public async Task IsMonitoring_ReturnsFalse_WhenNotStarted()
    {        
        var sensor = new AvaloniaAccelerometer();
        Assert.False(sensor.IsMonitoring);
    }
}
