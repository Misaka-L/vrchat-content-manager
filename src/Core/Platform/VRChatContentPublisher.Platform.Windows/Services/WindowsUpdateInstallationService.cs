using System.Diagnostics;
using System.IO.Compression;
using Microsoft.Win32;
using VRChatContentPublisher.Platform.Abstraction.Services;

namespace VRChatContentPublisher.Platform.Windows.Services;

public sealed class WindowsUpdateInstallationService : IUpdateInstallationService
{
    private readonly bool _isUpdateInstallationSupported = CheckUpdateInstallationSupported();

    public bool IsUpdateInstallationSupported() => _isUpdateInstallationSupported;
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
            $"-Command \"Write-Output 'Waiting for app shutdown'; Wait-Process -Id {Environment.ProcessId}; Start-Process '{installerPath}' -ArgumentList \\\"-start-app-after-install /S\\\"\""
        );
    }

    private static bool CheckUpdateInstallationSupported()
    {
        try
        {
            var currentExecutableDirectory = GetCurrentExecutableDirectory();
            if (currentExecutableDirectory is null)
                return false;

            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
            using var uninstallKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\VRChat Content Publisher");

            var installLocation = uninstallKey?.GetValue("InstallLocation") as string;
            if (string.IsNullOrWhiteSpace(installLocation))
                return false;

            return IsSameOrChildDirectory(currentExecutableDirectory, installLocation);
        }
        catch
        {
            return false;
        }
    }

    private static string? GetCurrentExecutableDirectory()
    {
        var processPath = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(processPath))
            return Path.GetDirectoryName(processPath);

        return AppContext.BaseDirectory;
    }

    private static bool IsSameOrChildDirectory(string candidatePath, string rootPath)
    {
        var normalizedCandidatePath = NormalizeDirectoryPath(candidatePath);
        var normalizedRootPath = NormalizeDirectoryPath(rootPath);

        return normalizedCandidatePath.StartsWith(normalizedRootPath, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeDirectoryPath(string path)
    {
        var fullPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(path.Trim()));

        if (Path.EndsInDirectorySeparator(fullPath))
            return fullPath;

        return fullPath + Path.DirectorySeparatorChar;
    }
}