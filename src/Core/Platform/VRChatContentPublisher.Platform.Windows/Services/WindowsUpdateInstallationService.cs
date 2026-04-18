using System.Diagnostics;
using System.IO.Compression;
using VRChatContentPublisher.Platform.Abstraction.Services;

namespace VRChatContentPublisher.Platform.Windows.Services;

public sealed class WindowsUpdateInstallationService : IUpdateInstallationService
{
    public bool IsUpdateInstallationSupported() => true;
    public string GetPlatformIdentifier() => "win-x64";

    public async ValueTask InstallUpdateAsync(string pathToBundle)
    {
        if (!File.Exists(pathToBundle))
            throw new ArgumentException("Provided bundle path does not exist", nameof(pathToBundle));

        var targetPath = Path.Combine(Path.GetTempPath(), "vrchat-content-publisher-updater-4df83857", "extract");
        var installerPath = Path.Combine(targetPath, "vrchat-content-publisher-installer.exe");

        if (Directory.Exists(targetPath))
            Directory.Delete(targetPath, true);

        Directory.CreateDirectory(targetPath);

        var installerZip = await ZipFile.OpenReadAsync(pathToBundle);
        await installerZip.ExtractToDirectoryAsync(targetPath);

        if (!File.Exists(installerPath))
            throw new FileNotFoundException("Installer not exist in extracted archive", installerPath);

        using var process = Process.Start("powershell",
            $"-Command \"Write-Output 'Waiting for app shutdown'; Wait-Process -Id {Environment.ProcessId}; Start-Process '{installerPath}'\""
        );
    }
}