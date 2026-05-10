using Avalonia.Controls.Maui.Essentials;

namespace Avalonia.Controls.Maui.Tests.Services;

public class AvaloniaFlashLightTests
{
    [Fact]
    public async Task IsSupported_Returns_False()
    {        
        var flash = new AvaloniaFlashlight();
        Assert.False(await flash.IsSupportedAsync());
    }

    [Fact]
    public async Task TurnOnAsync_ThrowsInvalidOperationException()
    {
        var flash = new AvaloniaFlashlight();

        await Assert.ThrowsAsync<InvalidOperationException>(flash.TurnOnAsync);
    }

    [Fact]
    public async Task TurnOffAsync_ThrowsInvalidOperationException()
    {
        var flash = new AvaloniaFlashlight();

        await Assert.ThrowsAsync<InvalidOperationException>(flash.TurnOffAsync);
    }
}
