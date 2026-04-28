using System.Runtime.InteropServices.JavaScript;

namespace Avalonia.Controls.Maui.Essentials
{
    /// <summary>
    /// Static class responsible for loading and managing the JavaScript sensor module
    /// used for WebAssembly sensor interop. This class ensures that the required
    /// JavaScript module is loaded exactly once, with thread safety and proper
    /// error handling.
    /// </summary>
    /// <remarks>
    /// The module loading follows a lazy initialization pattern:
    /// - First call to EnsureModuleLoadedAsync triggers the module load
    /// - Subsequent calls return the same Task (no duplicate loading)
    /// - Failed loads reset the state to allow retry attempts
    /// - Thread synchronization ensures safe concurrent access
    /// </remarks>
    static class JSSensors
    {
        /// <summary>
        /// The module name used to reference the JavaScript sensor module.
        /// This name must match the export name in the JavaScript file
        /// and is used by JSImport attributes to locate the module.
        /// </summary>
        public const string ModuleName = "sensors";

        /// <summary>
        /// The relative path to the JavaScript sensor module file.
        /// This file contains all the sensor interop implementations
        /// (accelerometer, compass, gyroscope, magnetometer, orientation).
        /// The path is relative to the application's base URL.
        /// </summary>
        const string ModulePath = "/sensors.browser.js";

        /// <summary>
        /// Lock object for thread-safe module loading operations.
        /// Prevents race conditions when multiple threads attempt to load
        /// the module simultaneously.
        /// </summary>
        static readonly object ModuleLoadLock = new();

        /// <summary>
        /// Indicates whether the JavaScript module has been successfully loaded.
        /// Initially false, set to true after successful module import.
        /// </summary>
        static bool _moduleLoaded;

        /// <summary>
        /// The task representing the ongoing module loading operation.
        /// Used to await the same loading task from multiple callers without
        /// re-loading the module. Null if no loading operation is in progress.
        /// </summary>
        static Task? _moduleLoadTask;

        /// <summary>
        /// Ensures the JavaScript sensor module is loaded and ready for use.
        /// This method can be called multiple times safely; it will only load
        /// the module once, even when called concurrently from multiple threads.
        /// </summary>
        /// <returns>
        /// A task that completes when the module is loaded and ready.
        /// - If already loaded, returns a completed task immediately
        /// - If loading is in progress, returns the existing loading task
        /// - If not loaded, initiates loading and returns the new task
        /// </returns>
        /// <remarks>
        /// This method is thread-safe and can be called from any thread.
        /// The module path is "/sensors.browser.js" which should be served
        /// as a static file from the application's web root.
        /// </remarks>
        public static Task EnsureModuleLoadedAsync()
        {
            lock (ModuleLoadLock)
            {
                // Fast path: module already loaded, return completed task
                if (_moduleLoaded)
                    return Task.CompletedTask;

                // If not loaded and no loading task exists, create one
                // If loading is in progress, return the existing task
                // This ensures only one loading operation occurs
                _moduleLoadTask ??= LoadModuleAsync();
                return _moduleLoadTask;
            }
        }

        /// <summary>
        /// Loads the JavaScript sensor module asynchronously.
        /// This method performs the actual module import operation using
        /// the JavaScript interop host and handles success and error states.
        /// </summary>
        /// <returns>A task representing the asynchronous loading operation.</returns>
        /// <remarks>
        /// On success: 
        /// - The module is imported via JSHost.ImportAsync
        /// - _moduleLoaded is set to true for fast path checks
        /// - Future calls to EnsureModuleLoadedAsync will return immediately
        /// 
        /// On failure:
        /// - _moduleLoadTask is reset to null to allow retry attempts
        /// - The module won't be stuck in a failed state permanently
        /// - Exception is re-thrown to notify callers of the failure
        /// 
        /// The module path is configured via ModulePath constant.
        /// ConfigureAwait(false) is used to avoid deadlocks when called from UI contexts.
        /// </remarks>
        public static async Task LoadModuleAsync()
        {
            try
            {
                // Import the JavaScript module from the specified path
                // The module will be available under the specified module name
                // The module file must be accessible as a static resource
                await JSHost.ImportAsync(ModuleName, ModulePath).ConfigureAwait(false);

                // Mark module as loaded successfully in a thread-safe manner
                lock (ModuleLoadLock)
                {
                    _moduleLoaded = true;
                }
            }
            catch
            {
                // On loading failure, reset the loading task so future calls can retry
                // This is critical because the module might be available later
                // (e.g., after network connectivity is restored)
                lock (ModuleLoadLock)
                {
                    _moduleLoadTask = null;
                }

                // Re-throw the exception to notify callers of the failure
                throw;
            }
        }
    }
}