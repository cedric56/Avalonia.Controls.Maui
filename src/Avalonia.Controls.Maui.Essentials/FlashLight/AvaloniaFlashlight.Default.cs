namespace Avalonia.Controls.Maui.Essentials;

partial class AvaloniaFlashlight
{
    const string LedsPath = "/sys/class/leds";
    string? _ledDirectory;

    public AvaloniaFlashlight()
    {
        if (Directory.Exists(LedsPath))
        {
            foreach (var dir in Directory.GetDirectories(LedsPath))
            {
                var name = Path.GetFileName(dir).ToLowerInvariant();
                if (name.Contains("torch") || name.Contains("flash"))
                    _ledDirectory = dir;
            }
        }
    }

    Task<bool> PlatformIsSupportedAsync() =>
        Task.FromResult(_ledDirectory != null);

    async Task PlatformTurnOffAsync() =>
        await File.WriteAllTextAsync(Path.Combine(_ledDirectory!, "brightness"), "0");

    async Task PlatformTurnOnAsync() =>
        await File.WriteAllTextAsync(Path.Combine(_ledDirectory!, "brightness"), "1");
}
