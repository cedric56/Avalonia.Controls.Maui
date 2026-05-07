extern alias TmdsDBus;

using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Maui.ApplicationModel;
using System.Diagnostics;
using System.Reflection;
using TmdsDBus::Tmds.DBus;

namespace Avalonia.Controls.Maui.Essentials
{
    /// <summary>
    /// Defines the D-Bus interface used to trigger app actions externally.
    /// This allows Linux desktop environments to invoke actions via D-Bus.
    /// </summary>
    [DBusInterface("com.essentials.AppActions")]
    public interface IAppActionsDBus : IDBusObject
    {
        /// <summary>
        /// Invoked by D-Bus when an app action is triggered externally.
        /// </summary>
        /// <param name="arg">Serialized argument representing the app action.</param>
        Task OnAppActionAsync(string arg);
    }

    partial class AvaloniaAppActions
    {
        /// <summary>
        /// Indicates whether the current platform supports this implementation.
        /// Only Linux is supported (D-Bus + desktop actions).
        /// </summary>
        public bool PlatformIsSupported() => OperatingSystem.IsLinux();

        /// <summary>
        /// Registers app actions on Linux using D-Bus and .desktop files.
        /// </summary>
        public async Task PlatformSetAsync(IEnumerable<AppAction> actions)
        {
            if (Application.Current?.ApplicationLifetime == null)
                throw new InvalidOperationException("ApplicationLifetime is not initialized");

            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                throw new InvalidOperationException("Only IClassicDesktopStyleApplicationLifetime is supported");

            // We create a new instance of the Current class, to get a unique WMClass
            // this will force refreshing shortcuts in the desktop environment
            var current = new Current(desktop, actions, AppActionActivated);
            await current.InitializeAsync();
        }

        /// <summary>
        /// Internal class responsible for:
        /// - Registering D-Bus service
        /// - Creating desktop entry files
        /// - Handling action invocation
        /// </summary>
        class Current(IClassicDesktopStyleApplicationLifetime desktop, IEnumerable<AppAction> actions, EventHandler<AppActionEventArgs>? onAppAction) : IAppActionsDBus
        {
            const string InterfaceName = "com.essentials.";

            // D-Bus object path required by IDBusObject
            ObjectPath IDBusObject.ObjectPath => new(currentObjectPath);

            // Unique ID per app instance to avoid collisions
            string uid = Guid.NewGuid().ToString("N");

            // Unique identifiers for D-Bus service and object
            string identifier => "AppActions" + uid;
            string currentObjectPath => "/com/essentials/" + identifier;
            string currentInterface => InterfaceName + identifier;
            string currentServiceBus => currentInterface + ".service";

            /// <summary>
            /// Initializes D-Bus service and desktop integration.
            /// </summary>
            public async Task InitializeAsync()
            {                
                // Resolve dotnet executable path
                var dotnet = InstallPath().Replace("\n", string.Empty);

                if (string.IsNullOrWhiteSpace(dotnet))
                    throw new Exception("Unable to find installed dotnet path");

                //When using ConfigureEssentials
                //We need the main window to set the WM_CLASS property,
                //so we wait until it's initialized
                var window = await WaitForMainWindowAsync();

                // Set WM_CLASS so desktop environments associate actions with the window
                X11Properties.SetWmClass(window, uid);

                var dll = AppContext.BaseDirectory;

                // Register D-Bus service and desktop actions
                await RegisterDBusService(dotnet, dll);
                await RegisterDesktopFiles(actions, dotnet, dll);
            }

            /// <summary>
            /// Asynchronously waits for the application's main window to be initialized.
            /// </summary>
            /// <param name="timeoutMs">Maximum time to wait for the main window, in milliseconds. Default is 10000.</param>
            /// <returns>A task that completes with the application's main Window once it becomes available.</returns>
            /// <exception cref="TimeoutException">Thrown if the main window is not initialized before the specified timeout elapses.</exception>
            private async Task<Window> WaitForMainWindowAsync(int timeoutMs = 10000)
            {
                var elapsed = 0;
                const int delay = 50;

                while (desktop.MainWindow == null)
                {
                    await Task.Delay(delay);
                    elapsed += delay;

                    if (elapsed >= timeoutMs)
                        throw new TimeoutException("MainWindow was not initialized in time");
                }

                return desktop.MainWindow;
            }

            /// <summary>
            /// Registers the D-Bus service and exposes this object.
            /// Also writes a .service file so the session bus can activate it.
            /// </summary>
            private async Task RegisterDBusService(string dotnet, string dll)
            {
                var dbusFile = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    $".local/share/dbus-1/services/{currentServiceBus}"
                );

                // D-Bus service definition file
                var dbus = $@"[D-BUS Service]
Name={currentInterface}
Exec={dotnet} {dll}
";

                var directory = Path.GetDirectoryName(dbusFile);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory!);

                await File.WriteAllTextAsync(dbusFile, dbus);

                // Ensure correct permissions
                Process.Start("chmod", $"644 {dbusFile}");

                // Connect to session bus and register object + service
                var connection = new Connection(Address.Session!);
                await connection.ConnectAsync();
                await connection.RegisterObjectAsync(this);
                await connection.RegisterServiceAsync(currentInterface, ServiceRegistrationOptions.ReplaceExisting);

                // Cleanup on app exit
                desktop.Exit += Desktop_Exit;

                void Desktop_Exit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
                {
                    desktop.Exit -= Desktop_Exit;
                    connection.Dispose();

                    if (File.Exists(dbusFile))
                        File.Delete(dbusFile);
                }
            }

            /// <summary>
            /// Creates a .desktop file with custom actions.
            /// These appear in Linux desktop menus (e.g., right-click app icon).
            /// </summary>
            private async Task RegisterDesktopFiles(IEnumerable<AppAction> actions, string dotnet, string dll)
            {
                var name = Assembly.GetEntryAssembly()?.GetName().Name;

                string desktopFile = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    $".local/share/applications/{name}{uid}.desktop"
                );

                // Base desktop entry
                string content = $@"[Desktop Entry]
Name={name}
Exec={dotnet} {dll} {uid}
Type=Application
Categories=Utility;
StartupNotify=true
StartupWMClass={uid}
Actions={string.Join(";", actions.Select(a => a.Title))};
";

                // Add individual actions
                foreach (var action in actions)
                {
                    content += $@"[Desktop Action {action.Title}]
Name={action.Title}
Exec=gdbus call --session --dest {currentInterface} --object-path {currentObjectPath} --method com.essentials.AppActions.OnAppAction ""{action.Id}""
";
                }

                await File.WriteAllTextAsync(desktopFile, content);

                // Make file executable
                Process.Start("chmod", $"+x {desktopFile}");

                // Cleanup on exit
                desktop.Exit += Desktop_Exit;

                void Desktop_Exit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
                {
                    desktop.Exit -= Desktop_Exit;

                    if (File.Exists(desktopFile))
                        File.Delete(desktopFile);
                }
            }

            /// <summary>
            /// Called when a D-Bus app action is triggered.
            /// Converts the argument back into an AppAction and raises the event.
            /// </summary>
            public Task OnAppActionAsync(string arg)
            {
                var action = actions?.FirstOrDefault(a => a.Id == arg);
                if (action != null)
                    onAppAction?.Invoke(this, new AppActionEventArgs(action));

                return Task.CompletedTask;
            }

            /// <summary>
            /// Finds the installed dotnet executable using 'which dotnet'.
            /// Required to launch the app from desktop/D-Bus.
            /// </summary>
            private static string InstallPath()
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "which",
                    Arguments = "dotnet",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);

                if (process != null)
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    return output;
                }

                return string.Empty;
            }
        }
    }
}