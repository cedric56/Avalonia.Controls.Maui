using Avalonia;
using Avalonia.Controls.Maui.Essentials;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Storage;

/// <summary>
/// Extension methods for configuring Avalonia-based Microsoft.Maui.Essentials services.
/// </summary>
public static class MauiEssentialsBuilderExtensions
{
    /// <summary>
    /// Configures the app to use Avalonia-based implementations of Microsoft.Maui.Essentials services.
    /// This is required to use any of the Essentials features in an Avalonia.Controls.Maui app with the default static instance.
    /// </summary>
    /// <param name="builder">The MauiAppBuilder instance.</param>
    /// <returns>The updated MauiAppBuilder instance.</returns>
    public static MauiAppBuilder UseAvaloniaEssentials(this MauiAppBuilder builder)
    {
        var platformProvider = new Avalonia.Controls.Maui.Essentials.AvaloniaEssentialsPlatformProvider();

        Microsoft.Maui.Media.Screenshot.SetDefault(new Avalonia.Controls.Maui.Essentials.AvaloniaScreenshot(platformProvider));
        Microsoft.Maui.Storage.FilePicker.SetDefault(new Avalonia.Controls.Maui.Essentials.AvaloniaFilePicker(platformProvider));
        Microsoft.Maui.Media.MediaPicker.SetDefault(new Avalonia.Controls.Maui.Essentials.AvaloniaMediaPicker(platformProvider));
        Microsoft.Maui.Devices.HapticFeedback.SetDefault(new Avalonia.Controls.Maui.Essentials.AvaloniaHapticFeedback());
        Microsoft.Maui.Storage.Preferences.SetDefault(new Avalonia.Controls.Maui.Essentials.AvaloniaPreferences());
        FileSystem.SetCurrent(new Avalonia.Controls.Maui.Essentials.AvaloniaFileSystem());
        Microsoft.Maui.Authentication.WebAuthenticator.SetDefault(new Avalonia.Controls.Maui.Essentials.AvaloniaWebAuthenticator(platformProvider));

        Microsoft.Maui.Devices.Sensors.Accelerometer.SetDefault(
#if !BROWSER
           OperatingSystem.IsWindowsVersionAtLeast(10, 0, 10240) ?
           new WindowsAccelerometer() :
           OperatingSystem.IsLinux() ?
           new LinuxAccelerometer() :
#endif
           new AvaloniaAccelerometer()
           );

        Microsoft.Maui.Devices.Sensors.Barometer.SetDefault(
#if !BROWSER
            OperatingSystem.IsWindowsVersionAtLeast(10, 0, 10240) ?
            new WindowsBarometer() :
            OperatingSystem.IsLinux() ?
            new LinuxBarometer() :
#endif
            new AvaloniaBarometer()
            );

        Microsoft.Maui.Devices.Sensors.Compass.SetDefault(
#if !BROWSER
            OperatingSystem.IsWindowsVersionAtLeast(10, 0, 10240) ?
            new WindowsCompass() :
            OperatingSystem.IsLinux() ?
            new LinuxCompass() :
#endif
            new AvaloniaCompass()
            );

        Microsoft.Maui.Devices.Sensors.Gyroscope.SetDefault(
#if !BROWSER
           OperatingSystem.IsWindowsVersionAtLeast(10, 0, 10240) ?
           new WindowsGyroscope() :
           OperatingSystem.IsLinux() ?
           new LinuxGyroscope() :
#endif
           new AvaloniaGyroscope()
           );

        Microsoft.Maui.Devices.Sensors.Magnetometer.SetDefault(
#if !BROWSER
           OperatingSystem.IsWindowsVersionAtLeast(10, 0, 10240) ?
           new WindowsMagnetometer() :
           OperatingSystem.IsLinux() ?
           new LinuxMagnetometer() :
#endif
           new AvaloniaMagnetometer()
           );

        Microsoft.Maui.Devices.Sensors.OrientationSensor.SetDefault(
#if !BROWSER
        OperatingSystem.IsWindowsVersionAtLeast(10, 0, 10240) ?
        new WindowsOrientationSensor() :
        OperatingSystem.IsLinux() ?
        new LinuxOrientationSensor() :
#endif
    new AvaloniaOrientationSensor()
    );

        return builder;
    }

    /// <summary>
    /// Configures the app to use Avalonia-based implementations of Microsoft.Maui.Essentials services.
    /// This is required to use any of the Essentials features in an Avalonia.Controls.Maui app with the default static instance.
    /// </summary>
    /// <param name="builder">The AppBuilder instance.</param>
    /// <returns>The updated AppBuilder instance.</returns>
    public static AppBuilder UseAvaloniaEssentials(this AppBuilder builder)
    {
        var platformProvider = new Avalonia.Controls.Maui.Essentials.AvaloniaEssentialsPlatformProvider();

        Microsoft.Maui.Media.Screenshot.SetDefault(new Avalonia.Controls.Maui.Essentials.AvaloniaScreenshot(platformProvider));
        Microsoft.Maui.Storage.FilePicker.SetDefault(new Avalonia.Controls.Maui.Essentials.AvaloniaFilePicker(platformProvider));
        Microsoft.Maui.Media.MediaPicker.SetDefault(new Avalonia.Controls.Maui.Essentials.AvaloniaMediaPicker(platformProvider));
        Microsoft.Maui.Devices.HapticFeedback.SetDefault(new Avalonia.Controls.Maui.Essentials.AvaloniaHapticFeedback());
        Microsoft.Maui.Storage.Preferences.SetDefault(new Avalonia.Controls.Maui.Essentials.AvaloniaPreferences());
        FileSystem.SetCurrent(new Avalonia.Controls.Maui.Essentials.AvaloniaFileSystem());
        Microsoft.Maui.Authentication.WebAuthenticator.SetDefault(new Avalonia.Controls.Maui.Essentials.AvaloniaWebAuthenticator(platformProvider));


        Microsoft.Maui.Devices.Sensors.Accelerometer.SetDefault(
#if !BROWSER
           OperatingSystem.IsWindowsVersionAtLeast(10, 0, 10240) ?
           new WindowsAccelerometer() :
           OperatingSystem.IsLinux() ?
           new LinuxAccelerometer() :
#endif
           new AvaloniaAccelerometer()
           );

        Microsoft.Maui.Devices.Sensors.Barometer.SetDefault(
#if !BROWSER
            OperatingSystem.IsWindowsVersionAtLeast(10, 0, 10240) ?
            new WindowsBarometer() :
            OperatingSystem.IsLinux() ?
            new LinuxBarometer() :
#endif
            new AvaloniaBarometer()
            );

        Microsoft.Maui.Devices.Sensors.Compass.SetDefault(
#if !BROWSER
            OperatingSystem.IsWindowsVersionAtLeast(10, 0, 10240) ?
            new WindowsCompass() :
            OperatingSystem.IsLinux() ?
            new LinuxCompass() :
#endif
            new AvaloniaCompass()
            );

        Microsoft.Maui.Devices.Sensors.Gyroscope.SetDefault(
#if !BROWSER
           OperatingSystem.IsWindowsVersionAtLeast(10, 0, 10240) ?
           new WindowsGyroscope() :
           OperatingSystem.IsLinux() ?
           new LinuxGyroscope() :
#endif
           new AvaloniaGyroscope()
           );

        Microsoft.Maui.Devices.Sensors.Magnetometer.SetDefault(
#if !BROWSER
           OperatingSystem.IsWindowsVersionAtLeast(10, 0, 10240) ?
           new WindowsMagnetometer() :
           OperatingSystem.IsLinux() ?
           new LinuxMagnetometer() :
#endif
           new AvaloniaMagnetometer()
           );

        Microsoft.Maui.Devices.Sensors.OrientationSensor.SetDefault(
#if !BROWSER
        OperatingSystem.IsWindowsVersionAtLeast(10, 0, 10240) ?
        new WindowsOrientationSensor() :
        OperatingSystem.IsLinux() ?
        new LinuxOrientationSensor() :
#endif
    new AvaloniaOrientationSensor()
    );

        return builder;
    }
}
