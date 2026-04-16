using System.Net.Http.Json;
using VRChatContentPublisher.App.Models.Update;
using VRChatContentPublisher.Core.Models.VRChatApi;

namespace VRChatContentPublisher.App.Services.Update;

public sealed class AppUpdateCheckService(
    IHttpClientFactory httpClientFactory
)
{
    private const string StableChannelUpdateApiUrl =
        "https://project-vrcz.misakal.xyz/api/content-publisher/updater.json";

    private const string PreviewChannelUpdateApiUrl =
        "https://project-vrcz.misakal.xyz/api/content-publisher/updater-beta.json";

    public async ValueTask<AppUpdateInformation> CheckForUpdateAsync()
    {
        using var httpClient = httpClientFactory.CreateClient();
        var information = await httpClient.GetFromJsonAsync<AppUpdateInformation>(
            GetUpdateApiUrl(),
            AppUpdateInformationJsonContext.Default.AppUpdateInformation
        ) ?? throw new UnexpectedApiBehaviourException("Update api returned null response");

        return information;
    }

    private string GetUpdateApiUrl()
    {
        return StableChannelUpdateApiUrl;
    }
}