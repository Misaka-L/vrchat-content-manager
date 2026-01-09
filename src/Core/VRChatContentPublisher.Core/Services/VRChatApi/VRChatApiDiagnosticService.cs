using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using VRChatContentPublisher.Core.Models.AtlassianStatusPage;
using VRChatContentPublisher.Core.Models.VRChatApi;
using VRChatContentPublisher.Core.Models.VRChatApi.Rest;

namespace VRChatContentPublisher.Core.Services.VRChatApi;

public sealed class VRChatApiDiagnosticService(
    HttpClient httpClient,
    ILogger<VRChatApiDiagnosticService> logger)
{
    private const string ApiStatusSummaryUrl = "https://status.vrchat.com/api/v2/summary.json";

    private const string CloudflareTraceUrl = "https://www.cloudflare.com/cdn-cgi/trace";
    private const string CloudflareChinaTraceUrl = "https://www.cloudflare-cn.com/cdn-cgi/trace";

    private const string ApiTraceUrl = "https://api.vrchat.cloud/cdn-cgi/trace";
    private const string AwsS3TestUrl = "https://s3.us-east-1.amazonaws.com/files.vrchat.cloud/";

    private const string ApiTestUrl = "https://api.vrchat.cloud/api/1/worlds/" + VRChatHomeWorldsId;
    private const string VRChatHomeWorldsId = "wrld_4432ea9b-729c-46e3-8eaf-846aa0a37fdd";

    public async ValueTask<StatusPageSummary> GetApiStatusSummaryAsync()
    {
        var response =
            await httpClient.GetFromJsonAsync(ApiStatusSummaryUrl, StatusPageJsonContext.Default.StatusPageSummary);

        if (response is null)
            throw new UnexpectedApiBehaviourException("The API returned a null status page summary.");

        return response;
    }

    public async ValueTask<string> GetCloudflareChinaTraceAsync()
    {
        try
        {
            using var response = await httpClient.GetAsync(CloudflareChinaTraceUrl);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return content;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get Cloudflare trace from Cloudflare China.");
            throw;
        }
    }

    public async ValueTask<string> GetCloudflareTraceAsync()
    {
        try
        {
            using var response = await httpClient.GetAsync(CloudflareTraceUrl);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return content;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get Cloudflare trace from Cloudflare.");
            throw;
        }
    }

    public async ValueTask<string> GetApiCloudflareTraceAsync()
    {
        try
        {
            using var response = await httpClient.GetAsync(ApiTraceUrl);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return content;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get Cloudflare trace from VRChat API.");
            throw;
        }
    }

    public async ValueTask TestAwsS3ConnectionAsync()
    {
        try
        {
            using var response = await httpClient.GetAsync(AwsS3TestUrl);

            if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.Forbidden)
                throw new UnexpectedApiBehaviourException("AWS S3 returned an unexpected status code: " +
                                                          response.StatusCode);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to connect to VRChat AWS S3 service.");
            throw;
        }
    }

    public async ValueTask<string> SendTestApiRequestAsync()
    {
        try
        {
            var response = await httpClient.GetAsync(ApiTestUrl);
            await HandleErrorResponseAsync(response);

            var world = await response.Content.ReadFromJsonAsync(ApiJsonContext.Default.VRChatApiWorld);
            if (world is null)
                throw new UnexpectedApiBehaviourException("The API returned a null world object.");

            return $"Got world data: {world.Id} - {world.Name}";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send test API request to VRChat API.");
            throw;
        }
    }

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