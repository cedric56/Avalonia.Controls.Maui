using Microsoft.Maui.ApplicationModel;
using System.Text;

namespace Avalonia.Controls.Maui.Essentials
{
    partial class AvaloniaAppActions : IAppActions
    {
        private IEnumerable<AppAction>? _actions;

#pragma warning disable CS0067
        public event EventHandler<AppActionEventArgs>? AppActionActivated;
#pragma warning disable CS0067

        public bool IsSupported => PlatformIsSupported();

        public Task<IEnumerable<AppAction>> GetAsync()
        {
            return Task.FromResult(_actions ??= Array.Empty<AppAction>());
        }

        public Task SetAsync(IEnumerable<AppAction> actions)
        {
            if (!IsSupported)
                throw new FeatureNotSupportedException();

            _actions = actions;

            return PlatformSetAsync(actions);
        }
    }
}
