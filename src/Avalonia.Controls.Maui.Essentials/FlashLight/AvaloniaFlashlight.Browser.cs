using System.Runtime.InteropServices.JavaScript;

namespace Avalonia.Controls.Maui.Essentials;

partial class AvaloniaFlashlight
{
    const string ModuleName = "flashlight";
    public AvaloniaFlashlight()
    {
        _ = JSHost.ImportAsync(ModuleName, "/flashlight.browser.js");
    }

    [JSImport("flashInterop.activate", ModuleName)]
    public static partial Task Activate(bool activate);

    [JSImport("flashInterop.isSupported", ModuleName)]
    public static partial Task<bool> IsSupported();

    Task<bool> PlatformIsSupportedAsync() =>
        IsSupported();

    async Task PlatformTurnOffAsync() =>
        await Activate(false);

    async Task PlatformTurnOnAsync() =>
        await Activate(true);
}
