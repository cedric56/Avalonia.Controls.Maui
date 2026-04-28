using Avalonia.Controls.Maui.Essentials;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using NSubstitute;

namespace Avalonia.Controls.Maui.Tests.Services
{
    public abstract class AvaloniaSensorTests<T> where T : AvaloniaSensor , new()
    {
        [Fact]
        public void IsSupported_ReturnsFalse()
        {
            var sensor = new T();
            Assert.False(sensor.IsSupported);
        }

        [Fact]
        public void IsSupported_ReturnsTrue()
        {
            var sensor = Substitute.For<T>();
            sensor.IsSupported.Returns(true);
            Assert.True(sensor.IsSupported);
        }

        [Fact]
        public void IsMonitoring_ReturnsFalse()
        {
            var sensor = new T();
            Assert.False(sensor.IsMonitoring);
        }

        [Fact]
        public void Start_WhenNotSupported_ShouldThrowFeatureNotSupportedException()
        {
            var sensor = new T();
            Assert.Throws<FeatureNotSupportedException>(() =>
                sensor.Start(SensorSpeed.Default));
        }

        [Fact]
        public void Stop_WhenNotSupported_ShouldThrowFeatureNotSupportedException()
        {
            var sensor = new T();
            Assert.Throws<FeatureNotSupportedException>(() => sensor.Stop());
        }

    }

    public class AvaloniaAccelerometerTests : AvaloniaSensorTests<AvaloniaAccelerometer> {

       

    }

    public class AvaloniaBarometerTests : AvaloniaSensorTests<AvaloniaBarometer>
    {



    }

    public class AvaloniaCompassTests : AvaloniaSensorTests<AvaloniaCompass>
    {



    }

    public class AvaloniaGyroscopeTests : AvaloniaSensorTests<AvaloniaGyroscope>
    {



    }

    public class AvaloniaMagnetometerTests : AvaloniaSensorTests<AvaloniaMagnetometer>
    {



    }

    public class AvaloniaOrientationSensorTests : AvaloniaSensorTests<AvaloniaOrientationSensor>
    {



    }
}
