using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using VRChatContentManager.Core.Models.VRChatApi;
using VRChatContentManager.Core.Models.VRChatApi.Rest;
using VRChatContentManager.Core.Models.VRChatApi.Rest.Auth;
using VRChatContentManager.Core.Models.VRChatApi.Rest.Avatars;
using VRChatContentManager.Core.Models.VRChatApi.Rest.Files;
using VRChatContentManager.Core.Models.VRChatApi.Rest.Worlds;
using VRChatContentManager.Core.Services.VRChatApi.S3;

namespace VRChatContentManager.Core.Services.VRChatApi;

public sealed partial class VRChatApiClient(
    HttpClient httpClient,
    ILogger<VRChatApiClient> logger,
    ConcurrentMultipartUploaderFactory concurrentMultipartUploaderFactory)
{
    public async ValueTask<CurrentUser> GetCurrentUser()
    {
        var response = await httpClient.GetAsync("auth/user");

        await HandleErrorResponseAsync(response);

        var user = await response.Content.ReadFromJsonAsync(ApiJsonContext.Default.CurrentUser);
        if (user is null)
            throw new UnexpectedApiBehaviourException("The API response a null user object.");

        return user;
    }

    public async ValueTask<LoginResult> LoginAsync(string username, string password)
    {
        var token = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{Uri.EscapeDataString(username)}:{Uri.EscapeDataString(password)}"));

        var request = new HttpRequestMessage(HttpMethod.Get, "auth/user")
        {
            Headers =
            {
                Authorization = new AuthenticationHeaderValue("Basic", token)
            }
        };

        var response = await httpClient.SendAsync(request);

        await HandleErrorResponseAsync(response);

        var content = await response.Content.ReadAsStringAsync();
        var responseJson = JsonNode.Parse(content);

        if (responseJson is null)
            throw new UnexpectedApiBehaviourException("The API returned a null json response.");

        if (responseJson["requiresTwoFactorAuth"] is { } twoFactorAuthField)
        {
            if (twoFactorAuthField.GetValueKind() != JsonValueKind.Array)
                throw new UnexpectedApiBehaviourException(
                    "The API returned a json response with not array requiresTwoFactorAuth field.");

            var requires2FaResponse =
                responseJson.Deserialize(ApiJsonContext.Default.RequireTwoFactorAuthResponse);
            return new LoginResult(false, requires2FaResponse!.RequiresTwoFactorAuth);
        }

        return new LoginResult(true, []);
    }

    public async ValueTask<bool> VerifyOtpAsync(string code, bool isEmailOtp = false)
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            isEmailOtp ? "auth/twofactorauth/emailotp/verify" : "auth/twofactorauth/totp/verify")
        {
            Content = JsonContent.Create(new VerifyTotpRequest(code), ApiJsonContext.Default.VerifyTotpRequest)
        };

        var response = await httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.BadRequest)
        {
            var responseJson = JsonNode.Parse(content);
            if (responseJson is null)
                throw new UnexpectedApiBehaviourException("The API returned a null json response.");

            if (responseJson["verified"] is { } verifiedField)
            {
                if (verifiedField.GetValueKind() != JsonValueKind.False &&
                    verifiedField.GetValueKind() != JsonValueKind.True)
                    throw new UnexpectedApiBehaviourException(
                        "The API returned a json response with not boolean verified field.");

                return verifiedField.GetValue<bool>();
            }
        }

        if (!response.IsSuccessStatusCode)
            HandleErrorResponse(content);

        throw new UnexpectedApiBehaviourException(
            $"The API returned a json response without verified field which status code {response.StatusCode}.");
    }

    public async Task LogoutAsync()
    {
        var response = await httpClient.PutAsync("logout", null);
        await HandleErrorResponseAsync(response);
    }

    #region Avatars

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

    #endregion

    #region Worlds

    public async ValueTask<VRChatApiWorld> GetWorldAsync(string worldId)
    {
        var response = await httpClient.GetAsync($"worlds/{worldId}");

        await HandleErrorResponseAsync(response);

        var world = await response.Content.ReadFromJsonAsync(ApiJsonContext.Default.VRChatApiWorld);
        if (world is null)
            throw new UnexpectedApiBehaviourException("The API returned a null world object.");

        return world;
    }

    public async ValueTask<VRChatApiWorld> CreateWorldVersionAsync(string worldId,
        CreateWorldVersionRequest createRequest, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var request = new HttpRequestMessage(HttpMethod.Put, $"worlds/{worldId}")
        {
            Content = JsonContent.Create(createRequest, ApiJsonContext.Default.CreateWorldVersionRequest)
        };

        var response = await httpClient.SendAsync(request, cancellationToken);

        await HandleErrorResponseAsync(response);

        var world = await response.Content.ReadFromJsonAsync(ApiJsonContext.Default.VRChatApiWorld, cancellationToken);
        if (world is null)
            throw new UnexpectedApiBehaviourException("The API returned a null world object.");

        return world;
    }

    #endregion

    #region Files

    public async ValueTask<VRChatApiFile> GetFileAsync(string fileId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var response = await httpClient.GetAsync($"file/{fileId}", cancellationToken);
        await HandleErrorResponseAsync(response);

        var file = await response.Content.ReadFromJsonAsync(ApiJsonContext.Default.VRChatApiFile,
            cancellationToken: cancellationToken);
        if (file is null)
            throw new UnexpectedApiBehaviourException("The API returned a null file.");

        return file;
    }

    public async ValueTask<VRChatApiFileVersion> CreateFileVersionAsync(
        string fileId,
        string fileMd5,
        long fileSizeInBytes,
        string signatureMd5,
        long signatureSizeInBytes,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var request = new HttpRequestMessage(HttpMethod.Post, "file/" + fileId)
        {
            Content = JsonContent.Create(
                new CreateFileVersionRequest(fileMd5, fileSizeInBytes, signatureMd5, signatureSizeInBytes),
                ApiJsonContext.Default.CreateFileVersionRequest)
        };

        var response = await httpClient.SendAsync(request, cancellationToken);
        await HandleErrorResponseAsync(response);

        var file = await response.Content.ReadFromJsonAsync(ApiJsonContext.Default.VRChatApiFile,
            cancellationToken: cancellationToken);
        if (file is null)
            throw new UnexpectedApiBehaviourException("The API returned a null file.");

        var latestVersion = file.Versions.MaxBy(v => v.Version);
        if (latestVersion is null)
            throw new UnexpectedApiBehaviourException("The API returned a file without versions.");

        if (latestVersion.Version == 0)
            throw new UnexpectedApiBehaviourException(
                "The API returned a file with no version created (only version 0).");

        return latestVersion;
    }

    public async ValueTask DeleteFileVersionAsync(string fileId, long versionId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var response = await httpClient.DeleteAsync($"file/{fileId}/{versionId}", cancellationToken);
        await HandleErrorResponseAsync(response);
    }

    public async ValueTask<FileVersionUploadStatus> GetFileVersionUploadStatusAsync(string fileId, int version,
        VRChatApiFileType fileType = VRChatApiFileType.File)
    {
        var response = await httpClient.GetAsync($"file/{fileId}/version/{version}/{fileType.ToApiString()}/status");

        await HandleErrorResponseAsync(response);

        var status = await response.Content.ReadFromJsonAsync(ApiJsonContext.Default.FileVersionUploadStatus);
        if (status is null)
            throw new UnexpectedApiBehaviourException("The API returned a null file version upload status.");

        return status;
    }

    public async ValueTask<string> GetSimpleUploadUrlAsync(string fileId, int version,
        VRChatApiFileType fileType = VRChatApiFileType.File, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var request = new HttpRequestMessage(HttpMethod.Put, $"file/{fileId}/{version}/{fileType.ToApiString()}/start");
        var response = await httpClient.SendAsync(request, cancellationToken);

        await HandleErrorResponseAsync(response);

        var uploadUrl =
            await response.Content.ReadFromJsonAsync(ApiJsonContext.Default.FileUploadUrlResponse, cancellationToken);

        if (uploadUrl is null)
            throw new UnexpectedApiBehaviourException("The API returned a null file upload url object.");

        return uploadUrl.Url;
    }

    public async ValueTask<string> GetFilePartUploadUrlAsync(string fileId, int version, int partNumber = 1,
        VRChatApiFileType fileType = VRChatApiFileType.File)
    {
        var request = new HttpRequestMessage(HttpMethod.Put,
            $"file/{fileId}/{version}/{fileType.ToApiString()}/start?partNumber={partNumber}");
        var response = await httpClient.SendAsync(request);

        await HandleErrorResponseAsync(response);

        var uploadUrl = await response.Content.ReadFromJsonAsync(ApiJsonContext.Default.FileUploadUrlResponse);

        if (uploadUrl is null)
            throw new UnexpectedApiBehaviourException("The API returned a null file upload url object.");

        return uploadUrl.Url;
    }

    public async ValueTask CompleteSimpleFileUploadAsync(string fileId, int version,
        VRChatApiFileType fileType = VRChatApiFileType.File, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var request =
            new HttpRequestMessage(HttpMethod.Put, $"file/{fileId}/{version}/{fileType.ToApiString()}/finish");

        var response = await httpClient.SendAsync(request, cancellationToken);

        await HandleErrorResponseAsync(response);
    }

    public async ValueTask CompleteFilePartUploadAsync(string fileId, int version,
        string[]? eTags = null, VRChatApiFileType fileType = VRChatApiFileType.File,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var request =
            new HttpRequestMessage(HttpMethod.Put, $"file/{fileId}/{version}/{fileType.ToApiString()}/finish");

        if (eTags is not null)
        {
            if (fileType == VRChatApiFileType.Signature)
                throw new ArgumentException("ETags are not required for signature file type.", nameof(eTags));

            request.Content = JsonContent.Create(new CompleteFileUploadRequest(eTags),
                ApiJsonContext.Default.CompleteFileUploadRequest);
        }

        var response = await httpClient.SendAsync(request, cancellationToken);

        await HandleErrorResponseAsync(response);
    }

    #endregion

    private static async Task HandleErrorResponseAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        var content = await response.Content.ReadAsStringAsync();
        HandleErrorResponse(content);
    }

    private static void HandleErrorResponse(string response)
    {
        var errorResponse = JsonSerializer.Deserialize(response, ApiJsonContext.Default.ApiErrorResponse);

        if (errorResponse is null)
            throw new UnexpectedApiBehaviourException(
                "The API returned an error response that could not be deserialized.");

        throw new ApiErrorException(errorResponse.Message, errorResponse.StatusCode);
    }
}