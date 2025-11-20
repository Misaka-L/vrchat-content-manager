using System.Net.Http.Json;
using VRChatContentManager.Core.Models.VRChatApi;
using VRChatContentManager.Core.Models.VRChatApi.Rest;
using VRChatContentManager.Core.Models.VRChatApi.Rest.Avatars;

namespace VRChatContentManager.Core.Services.VRChatApi;

public sealed partial class VRChatApiClient
{
    public async ValueTask<VRChatApiAvatar> GetAvatarAsync(string avatarId,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync($"avatars/{avatarId}", cancellationToken);

        await HandleErrorResponseAsync(response);

        var avatar =
            await response.Content.ReadFromJsonAsync(ApiJsonContext.Default.VRChatApiAvatar, cancellationToken);
        if (avatar is null)
            throw new UnexpectedApiBehaviourException("The API returned a null avatar object.");

        return avatar;
    }

    public async ValueTask<VRChatApiAvatar> CreateAvatarVersionAsync(string avatarId,
        CreateAvatarVersionRequest createRequest, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var request = new HttpRequestMessage(HttpMethod.Put, $"avatars/{avatarId}")
        {
            Content = JsonContent.Create(createRequest, ApiJsonContext.Default.CreateAvatarVersionRequest)
        };

        var response = await httpClient.SendAsync(request, cancellationToken);

        await HandleErrorResponseAsync(response);

        var world = await response.Content.ReadFromJsonAsync(ApiJsonContext.Default.VRChatApiAvatar, cancellationToken);
        if (world is null)
            throw new UnexpectedApiBehaviourException("The API returned a null avatar object.");

        return world;
    }
}