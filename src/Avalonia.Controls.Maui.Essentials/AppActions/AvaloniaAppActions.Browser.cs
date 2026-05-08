using Microsoft.Maui.ApplicationModel;

namespace Avalonia.Controls.Maui.Essentials
{
    partial class AvaloniaAppActions
    {
        public bool PlatformIsSupported() => false;

        public Task<bool> PlatformSetAsync(IEnumerable<AppAction> actions) =>
            throw new FeatureNotSupportedException();
    }
}
