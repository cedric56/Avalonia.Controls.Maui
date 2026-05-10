using Microsoft.Maui.Devices;

namespace Avalonia.Controls.Maui.Essentials;

partial class AvaloniaFlashlight : IFlashlight
{
    public Task<bool> IsSupportedAsync() =>
        PlatformIsSupportedAsync();

    public async Task TurnOffAsync()
    {
        if (!await IsSupportedAsync())
            throw new InvalidOperationException();

        await PlatformTurnOffAsync();
    }


    public async Task TurnOnAsync()
    {
        if (!await IsSupportedAsync())
            throw new InvalidOperationException();


        await PlatformTurnOnAsync();
    }
}
