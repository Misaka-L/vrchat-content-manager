using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VRChatContentPublisher.ConnectCore.Extensions;
using VRChatContentPublisher.ConnectCore.Exceptions;
using VRChatContentPublisher.ConnectCore.Models.Api.V1;
using VRChatContentPublisher.ConnectCore.Results;
using VRChatContentPublisher.ConnectCore.Services.Connect;
using VRChatContentPublisher.ConnectCore.Services.PublishTask;

namespace VRChatContentPublisher.ConnectCore.Endpoints.V1;

public static class TaskEndpoint
{
    public static EndpointService MapTaskEndpoint(this EndpointService service)
    {
        service.Map("POST", "/v1/tasks/world", CreateWorldPublishTask);
        service.Map("POST", "/v1/tasks/avatar", CreateAvatarPublishTask);

        return service;
    }

    private static async Task<IEndpointResult> CreateWorldPublishTask(HttpContext context, IServiceProvider services)
    {
        if (await context.ReadJsonWithErrorHandleAsync(ApiV1JsonContext.Default.CreateWorldPublishTaskRequest) is not
            { } request)
            return EndpointResults.Problem(ApiV1ProblemType.Undocumented, StatusCodes.Status400BadRequest,
                "Bad Request", "Request body is null or invalid.");

        var loggerFactory = services.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger(nameof(TaskEndpoint));

        var worldPublishTaskService = services.GetRequiredService<IWorldPublishTaskService>();
        try
        {
            await worldPublishTaskService.CreatePublishTaskAsync(
                request.WorldId,
                request.WorldBundleFileId,
                request.Name,
                request.Platform,
                request.UnityVersion,
                request.AuthorId,
                request.WorldSignature,
                request.ThumbnailFileId,
                request.Description,
                request.Tags,
                request.ReleaseStatus,
                request.Capacity,
                request.RecommendedCapacity,
                request.PreviewYoutubeId,
                request.UdonProducts
            );

            return EndpointResults.NoContent();
        }
        catch (ProvideFileIdNotFoundException ex)
        {
            logger.LogError(ex, "Failed to create world publish task due to provided file ID not found.");
            return EndpointResults.Problem(
                ApiV1ProblemType.Undocumented,
                StatusCodes.Status400BadRequest,
                ex.Message
            );
        }
        catch (NoUserSessionAvailableException ex)
        {
            logger.LogError(ex, "Failed to create world publish task due to no user session available.");
            return EndpointResults.Problem(
                ApiV1ProblemType.Undocumented,
                StatusCodes.Status503ServiceUnavailable,
                ex.Message
            );
        }
        catch (ContentOwnerUserSessionNotFoundException ex)
        {
            logger.LogError(ex, "Failed to create world publish task due to content owner user session not found.");
            return EndpointResults.Problem(
                ApiV1ProblemType.Undocumented,
                StatusCodes.Status400BadRequest,
                ex.Message
            );
        }
        catch (ArgumentException ex)
        {
            logger.LogError(ex, "Failed to create world publish task due to invalid argument.");
            return EndpointResults.Problem(
                ApiV1ProblemType.Undocumented,
                StatusCodes.Status400BadRequest,
                ex.Message
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create world publish task.");
            return EndpointResults.Problem(
                ApiV1ProblemType.Undocumented,
                StatusCodes.Status500InternalServerError,
                "Internal Server Error", "An unexpected error occurred.");
        }
    }

    private static async Task<IEndpointResult> CreateAvatarPublishTask(HttpContext context, IServiceProvider services)
    {
        if (await context.ReadJsonWithErrorHandleAsync(ApiV1JsonContext.Default.CreateAvatarPublishTaskRequest) is not
            { } request)
            return EndpointResults.Problem(ApiV1ProblemType.Undocumented, StatusCodes.Status400BadRequest,
                "Bad Request", "Request body is null or invalid.");

        var loggerFactory = services.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger(nameof(TaskEndpoint));

        var avatarPublishTaskService = services.GetRequiredService<IAvatarPublishTaskService>();
        try
        {
            await avatarPublishTaskService.CreatePublishTaskAsync(
                request.AvatarId,
                request.AvatarBundleFileId,
                request.Name,
                request.Platform,
                request.UnityVersion,
                request.AuthorId,
                request.ThumbnailFileId,
                request.Description,
                request.Tags,
                request.ReleaseStatus);

            return EndpointResults.NoContent();
        }
        catch (ProvideFileIdNotFoundException ex)
        {
            logger.LogError(ex, "Failed to create avatar publish task due to provided file ID not found.");
            return EndpointResults.Problem(
                ApiV1ProblemType.Undocumented,
                StatusCodes.Status400BadRequest,
                ex.Message
            );
        }
        catch (NoUserSessionAvailableException ex)
        {
            logger.LogError(ex, "Failed to create avatar publish task due to no user session available.");
            return EndpointResults.Problem(
                ApiV1ProblemType.Undocumented,
                StatusCodes.Status503ServiceUnavailable,
                ex.Message
            );
        }
        catch (ContentOwnerSessionOrAvatarNotFoundException ex)
        {
            logger.LogError(ex, "Failed to create avatar publish task due to content owner user session not found or avatar not exist.");
            return EndpointResults.Problem(
                ApiV1ProblemType.Undocumented,
                StatusCodes.Status400BadRequest,
                ex.Message
            );
        }
        catch (ArgumentException ex)
        {
            logger.LogError(ex, "Failed to create avatar publish task due to invalid argument.");
            return EndpointResults.Problem(
                ApiV1ProblemType.Undocumented,
                StatusCodes.Status400BadRequest,
                ex.Message
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create avatar publish task.");
            return EndpointResults.Problem(
                ApiV1ProblemType.Undocumented,
                StatusCodes.Status500InternalServerError,
                "Internal Server Error", "An unexpected error occurred.");
        }
    }
}