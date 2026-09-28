using System.Net;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using VRChatContentPublisher.Core.AppServices;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;

namespace VRChatContentPublisher.App.ViewModels.NetworkDiagnostic;

/// <summary>
/// Read only snapshot of the proxy the app will actually use for its own HTTP requests.
/// <see cref="AppWebProxy"/> is installed on every HTTP handler the app creates
/// (general client, S3 upload clients, VRChat API session client, update download and telemetry),
/// so resolving through it describes the effective proxy for every destination below.
/// </summary>
public sealed partial class ProxyInfoViewModel(
    IWritableOptions<AppSettings> appSettings,
    AppWebProxy appWebProxy) : ViewModelBase
{
    private const string NotSetText = "(not set)";
    private const string DirectText = "Direct (no proxy)";

    private static readonly string[] ProbeDestinations =
    [
        "https://api.vrchat.cloud/",
        "https://s3.us-east-1.amazonaws.com/",
        "https://status.vrchat.com/",
        "https://www.cloudflare.com/",
        "https://www.cloudflare-cn.com/",
        "http://127.0.0.1/"
    ];

    private static readonly string[] ProxyEnvironmentVariableNames =
    [
        "HTTP_PROXY", "http_proxy",
        "HTTPS_PROXY", "https_proxy",
        "ALL_PROXY", "all_proxy",
        "NO_PROXY", "no_proxy"
    ];

    public AvaloniaList<ProxyResolutionViewModel> ProbeResults { get; } = [];

    [ObservableProperty] public partial string AppProxyMode { get; private set; } = NotSetText;
    [ObservableProperty] public partial string CustomProxyUri { get; private set; } = NotSetText;
    [ObservableProperty] public partial string ProxyInUseText { get; private set; } = "Unknown";
    [ObservableProperty] public partial string SystemProxySource { get; private set; } = NotSetText;
    [ObservableProperty] public partial string ProxyEnvironmentSummary { get; private set; } = NotSetText;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNotes))]
    public partial string Notes { get; private set; } = "";

    public bool HasNotes => !string.IsNullOrEmpty(Notes);

    /// <summary>
    /// Re-reads the app proxy settings and re-resolves every probe destination.
    /// Does not perform any network request.
    /// </summary>
    public void Refresh()
    {
        var notes = new List<string>();

        try
        {
            RefreshCore(notes);
        }
        catch (Exception ex)
        {
            notes.Add($"Failed to inspect proxy settings: {ex.Message}");
        }

        Notes = notes.Count == 0 ? "" : string.Join(Environment.NewLine, notes);
    }

    private void RefreshCore(ICollection<string> notes)
    {
        var proxySettings = appSettings.Value.HttpProxySettings;
        var customProxyUri = appSettings.Value.HttpProxyUri;

        AppProxyMode = DescribeProxyMode(proxySettings);
        CustomProxyUri = customProxyUri is null ? NotSetText : FormatProxyUri(customProxyUri);

        if (proxySettings == AppHttpProxySettings.CustomProxy && customProxyUri is null)
            notes.Add("Custom proxy mode is selected but no proxy URI is set, " +
                      "so the app falls back to the system proxy settings.");

        if (customProxyUri is not null && !IsHttpProxyUri(customProxyUri))
            notes.Add($"The custom proxy URI uses scheme '{customProxyUri.Scheme}', " +
                      "but only http and https URIs are accepted by the HTTP proxy settings.");

        var hasProxyEnvironmentVariable = DescribeProxyEnvironment(out var proxyEnvironmentSummary);
        ProxyEnvironmentSummary = proxyEnvironmentSummary;

        SystemProxySource = DescribeSystemProxySource(out var systemProxyUsesEnvironment, notes);

        if (proxySettings == AppHttpProxySettings.SystemProxy && systemProxyUsesEnvironment)
            notes.Add("The system proxy currently resolves through proxy environment variables. " +
                      "They are inherited from the environment the app was launched with.");

        if (hasProxyEnvironmentVariable && !systemProxyUsesEnvironment)
            notes.Add("Proxy environment variables are set, but they are not currently used to resolve " +
                      "the system proxy, so only the operating system proxy settings apply.");

        var probeResults = new List<ProxyResolutionViewModel>();
        var anyProxyInUse = false;

        foreach (var destinationText in ProbeDestinations)
        {
            if (!Uri.TryCreate(destinationText, UriKind.Absolute, out var destination))
            {
                probeResults.Add(new ProxyResolutionViewModel(destinationText, "(invalid destination)", "-"));
                continue;
            }

            try
            {
                var proxyUri = appWebProxy.GetProxy(destination);
                var bypassed = appWebProxy.IsBypassed(destination);

                // WebProxy returns the destination itself when the destination matches a bypass entry,
                // while HttpClient's environment proxy returns null for a bypassed destination.
                var isDirect = bypassed || proxyUri is null || proxyUri == destination;

                if (!isDirect && !destination.IsLoopback)
                    anyProxyInUse = true;

                probeResults.Add(new ProxyResolutionViewModel(
                    destinationText,
                    isDirect ? DirectText : FormatProxyUri(proxyUri!),
                    bypassed ? "Yes" : "No"));
            }
            catch (Exception ex)
            {
                probeResults.Add(new ProxyResolutionViewModel(destinationText,
                    $"Failed to resolve: {ex.Message}", "-"));
            }
        }

        ProbeResults.Clear();
        ProbeResults.AddRange(probeResults);

        ProxyInUseText = anyProxyInUse ? "Yes" : "No";

        if (!anyProxyInUse)
            notes.Add("No proxy is used for any of the destinations below, so the app connects directly.");
    }

    private static string DescribeProxyMode(AppHttpProxySettings proxySettings)
    {
        return proxySettings switch
        {
            AppHttpProxySettings.NoProxy => "No Proxy",
            AppHttpProxySettings.CustomProxy => "Custom Proxy",
            _ => "System Proxy (Follow System Settings)"
        };
    }

    /// <summary>
    /// Reports which of the proxy environment variables are set without revealing proxy credentials.
    /// </summary>
    private static bool DescribeProxyEnvironment(out string summary)
    {
        var parts = new List<string>();
        var hasAny = false;

        foreach (var name in ProxyEnvironmentVariableNames)
        {
            var value = Environment.GetEnvironmentVariable(name);

            if (string.IsNullOrWhiteSpace(value))
            {
                parts.Add($"{name}: not set");
                continue;
            }

            hasAny = true;
            parts.Add($"{name}: {SanitizeProxyString(value)}");
        }

        summary = string.Join(Environment.NewLine, parts);
        return hasAny;
    }

    private static string DescribeSystemProxySource(out bool usesEnvironmentVariable, ICollection<string> notes)
    {
        usesEnvironmentVariable = false;

        IWebProxy systemProxy;
        try
        {
            systemProxy = WebRequest.GetSystemWebProxy();
        }
        catch (Exception ex)
        {
            notes.Add($"Failed to query the system proxy: {ex.Message}");
            return "(unavailable)";
        }

        var typeName = systemProxy.GetType().Name;

        var description = typeName switch
        {
            "HttpEnvironmentProxy" => "Environment variables (HTTP_PROXY / HTTPS_PROXY / ALL_PROXY)",
            "HttpWindowsProxy" => "Windows system settings (WinINET, including PAC)",
            "WebProxy" => "Explicit WebProxy instance",
            _ => "Unknown system proxy implementation"
        };

        usesEnvironmentVariable = typeName == "HttpEnvironmentProxy";

        return $"{description} [{typeName}]";
    }

    private static bool IsHttpProxyUri(Uri uri)
    {
        return uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) ||
               uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatProxyUri(Uri uri)
    {
        // Discard the user info part so proxy credentials never show up in this window.
        var server = uri.GetComponents(UriComponents.SchemeAndServer, UriFormat.UriEscaped);

        return string.IsNullOrEmpty(uri.UserInfo)
            ? server
            : server + " (credentials configured)";
    }

    private static string SanitizeProxyString(string value)
    {
        value = value.Trim();

        if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
            return FormatProxyUri(uri);

        // Not a URI we can parse, so only strip anything that looks like "user:password@".
        var userInfoEnd = value.IndexOf('@');
        return userInfoEnd >= 0 ? "***" + value[userInfoEnd..] : value;
    }
}

public sealed class ProxyResolutionViewModel(string destination, string proxy, string bypassed)
{
    public string Destination => destination;
    public string Proxy => proxy;
    public string Bypassed => bypassed;
}