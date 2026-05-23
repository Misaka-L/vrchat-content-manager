using System.Globalization;
using System.Text.Json;

namespace VRChatContentPublisher.CrashHandler.App.Services;

public static class LocaleService
{
    private const string DefaultCulture = "en";
    private const string SettingsFileName = "settings.json";

    private static readonly string[] SupportedCultures = ["en", "zh-CN"];

    /// <summary>
    /// Resolves the culture to use:
    /// 1) Read <c>Settings.AppCulture</c> from the main app's <c>settings.json</c>
    /// 2) Fall back to system <see cref="CultureInfo.CurrentCulture"/>
    /// 3) Fall back to <c>"en"</c>
    /// </summary>
    public static CultureInfo ResolveCulture()
    {
        // 1) Try app settings
        var appCulture = ReadAppCultureFromSettings();
        if (appCulture is not null)
            return CultureInfo.GetCultureInfo(appCulture);

        // 2) Try system culture
        var systemCulture = CultureInfo.CurrentCulture;
        var systemName = systemCulture.Name; // e.g. "zh-CN", "en-US", "zh"
        var matched = MatchSupportedCulture(systemName);
        if (matched is not null)
            return CultureInfo.GetCultureInfo(matched);

        // 3) Fallback
        return CultureInfo.GetCultureInfo(DefaultCulture);
    }

    /// <summary>
    /// Apply the resolved culture to the current thread.
    /// </summary>
    public static void ApplyCulture()
    {
        var culture = ResolveCulture();
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
    }

    private static string? ReadAppCultureFromSettings()
    {
        try
        {
            var settingsPath = Path.Combine(AppPathUtils.GetStoragePath(), SettingsFileName);
            if (!File.Exists(settingsPath))
                return null;

            using var stream = File.OpenRead(settingsPath);
            using var doc = JsonDocument.Parse(stream);

            if (!doc.RootElement.TryGetProperty("Settings", out var settings))
                return null;

            if (!settings.TryGetProperty("AppCulture", out var cultureProp))
                return null;

            var cultureCode = cultureProp.GetString();
            if (string.IsNullOrWhiteSpace(cultureCode))
                return null;

            var matched = MatchSupportedCulture(cultureCode);
            return matched;
        }
        catch
        {
            // Failsafe: any error reading settings → fall through to next fallback
            return null;
        }
    }

    private static string? MatchSupportedCulture(string cultureName)
    {
        // Direct match
        foreach (var supported in SupportedCultures)
        {
            if (string.Equals(supported, cultureName, StringComparison.OrdinalIgnoreCase))
                return supported;
        }

        // Parent match (e.g. "zh" → "zh-CN", "en-US" → "en")
        try
        {
            var culture = CultureInfo.GetCultureInfo(cultureName);
            while (culture.Parent is not null && culture.Parent != CultureInfo.InvariantCulture)
            {
                foreach (var supported in SupportedCultures)
                {
                    if (string.Equals(supported, culture.Parent.Name, StringComparison.OrdinalIgnoreCase))
                        return supported;
                }
                culture = culture.Parent;
            }
        }
        catch
        {
            // ignore
        }

        return null;
    }
}
