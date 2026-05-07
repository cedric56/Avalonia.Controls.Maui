using Avalonia.Controls.Maui.Essentials;
using Microsoft.Maui.ApplicationModel;

namespace Avalonia.Controls.Maui.Tests.Services;

public class AvaloniaAppActionsTests
{
    [Fact]
    public void IsSupported_ReturnsFalse()
    {
        var appActions = new AvaloniaAppActions();
        Assert.False(appActions.IsSupported);
    }

    [Fact]
    public async Task GetAsync_ReturnsEmpty()
    {
        var appActions = new AvaloniaAppActions();
        Assert.Empty(await appActions.GetAsync());
    }

    [Fact]
    public async Task ThrowsFeatureNotSupportedException_OnSetAsync()
    {
        var appActions = new AvaloniaAppActions();
        await Assert.ThrowsAsync<FeatureNotSupportedException>(async () => await appActions.SetAsync(Array.Empty<AppAction>()));
    }
}
