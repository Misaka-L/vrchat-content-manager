using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using VRChatContentPublisher.App.Models.PrivacyPolicy;

namespace VRChatContentPublisher.App.Services.Telemetry.PrivacyPolicy;

public sealed class PrivacyPolicyService
{
    private const string TermsApiUrl =
        "https://project-vrcz.misakal.xyz/api/content-publisher/terms.json";

    private const string FallbackPolicyUrl =
        "https://github.com/project-vrcz/content-publisher/blob/main/docs/privacy/PRIVACY.md";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PrivacyPolicyService> _logger;

    private Task<PrivacyPolicyData>? _fetchTask;

    public PrivacyPolicyService(IHttpClientFactory httpClientFactory, ILogger<PrivacyPolicyService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Called by the prefetch hosted service to start fetching in parallel with app startup.
    /// </summary>
    public void StartPrefetch()
    {
        _fetchTask = FetchAsync();
    }

    /// <summary>
    /// Returns the latest privacy policy data. Awaits the prefetched task if already started,
    /// otherwise starts a new fetch.
    /// </summary>
    public Task<PrivacyPolicyData> GetLatestPrivacyPolicyAsync()
    {
        return _fetchTask ?? FetchAsync();
    }

    /// <summary>
    /// Gets the privacy policy URL localized to the current app culture.
    /// </summary>
    public string GetLocalizedPolicyUrl(PrivacyPolicyData data)
    {
        if (data.Language is null)
            return data.Url;

        var currentCulture = Thread.CurrentThread.CurrentCulture.Name;
        if (data.Language.TryGetValue(currentCulture, out var localizedUrl))
            return localizedUrl;

        return data.Url;
    }

    /// <summary>
    /// The latest privacy policy URL (already localized). Returns fallback URL
    /// before the first fetch completes.
    /// </summary>
    public string PrivacyPolicyUrl
    {
        get
        {
            if (_fetchTask is { IsCompletedSuccessfully: true })
                return GetLocalizedPolicyUrl(_fetchTask.Result);

            return FallbackPolicyUrl;
        }
    }

    private async Task<PrivacyPolicyData> FetchAsync()
    {
        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetFromJsonAsync(
                TermsApiUrl,
                PrivacyPolicyJsonContext.Default.PrivacyPolicyApiResponse
            );

            if (response?.PrivacyPolicy is { } policy)
            {
                _logger.LogInformation(
                    "Fetched latest privacy policy version {Version} from API",
                    policy.Version);
                return policy;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to fetch privacy policy from API, using fallback");
        }

        return CreateFallbackPolicy();
    }

    private static PrivacyPolicyData CreateFallbackPolicy()
    {
        return new PrivacyPolicyData(
            Version: 1,
            Url: FallbackPolicyUrl,
            Language: null
        );
    }
}
