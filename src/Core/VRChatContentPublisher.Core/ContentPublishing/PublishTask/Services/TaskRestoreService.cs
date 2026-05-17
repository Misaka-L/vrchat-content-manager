using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VRChatContentPublisher.Core.ContentPublishing.ContentPublisher;
using VRChatContentPublisher.Core.ContentPublishing.PublishTask.Models;
using VRChatContentPublisher.Core.Database;
using VRChatContentPublisher.Core.UserSession;

namespace VRChatContentPublisher.Core.ContentPublishing.PublishTask.Services;

/// <summary>
/// Restores persisted <see cref="ContentPublishTaskState"/> records from the database
/// into the <see cref="TaskManagerService"/> so they are visible in the UI.
/// Must be called after user sessions have been restored (see <c>BootstrapPageViewModel</c>).
/// </summary>
public sealed class TaskRestoreService(
    ContentPublishTaskDatabaseService databaseService,
    ILogger<TaskRestoreService> logger)
{
    public async Task RestoreTasksAsync(UserSessionManagerService sessionManager)
    {
        var states = await databaseService.GetAllStatesAsync();

        if (states.Count == 0)
        {
            logger.LogInformation("No persisted publish tasks to restore");
            return;
        }

        logger.LogInformation("Restoring {Count} persisted publish task(s)", states.Count);

        foreach (var state in states)
        {
            try
            {
                var userSession = FindUserSession(sessionManager, state.UserId);
                if (userSession is null)
                {
                    logger.LogWarning(
                        "No session found for user {UserId} — skipping task {TaskId}",
                        state.UserId, state.TaskId);
                    continue;
                }

                var scope = await userSession.CreateOrGetSessionScopeAsync();
                var taskManager = scope.ServiceProvider.GetRequiredService<TaskManagerService>();
                var publisher = CreatePublisherFromState(scope.ServiceProvider, userSession, state);

                await taskManager.RestoreTaskFromStateAsync(state, publisher);
                logger.LogDebug(
                    "Restored task {TaskId} ({ContentType} {ContentName})",
                    state.TaskId, state.ContentType, state.ContentName);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Failed to restore task {TaskId} ({ContentType} {ContentName}) — skipping",
                    state.TaskId, state.ContentType, state.ContentName);
            }
        }

        logger.LogInformation("Restored {Count} persisted publish task(s)", states.Count);
    }

    /// <summary>
    /// Finds the <see cref="UserSessionService"/> matching the given user ID
    /// from the session manager's active sessions.
    /// </summary>
    private static UserSessionService? FindUserSession(UserSessionManagerService sessionManager, string? userId)
    {
        if (string.IsNullOrEmpty(userId))
            return sessionManager.GetDefaultSession();

        return sessionManager.Sessions.FirstOrDefault(s => s.UserId == userId)
               ?? sessionManager.GetDefaultSession();
    }

    /// <summary>
    /// Reconstructs a real <see cref="IContentPublisher"/> from the serialized
    /// <see cref="ContentPublishTaskState.PublisherOptionsJson"/> using the
    /// appropriate factory.
    /// </summary>
    private static IContentPublisher CreatePublisherFromState(
        IServiceProvider services, UserSessionService userSession, ContentPublishTaskState state)
    {
        if (string.IsNullOrEmpty(state.PublisherOptionsJson))
            throw new InvalidOperationException(
                $"Task {state.TaskId} has no publisher options — cannot restore.");

        return state.ContentType switch
        {
            "world" => services.GetRequiredService<WorldContentPublisherFactory>()
                .Create(
                    userSession,
                    JsonSerializer.Deserialize(
                        state.PublisherOptionsJson,
                        ContentPublishTaskStateJsonContext.Default.WorldContentPublisherOptions)!),

            "avatar" => services.GetRequiredService<AvatarContentPublisherFactory>()
                .Create(
                    userSession,
                    JsonSerializer.Deserialize(
                        state.PublisherOptionsJson,
                        ContentPublishTaskStateJsonContext.Default.AvatarContentPublisherOptions)!),

            _ => throw new InvalidOperationException(
                $"Unknown content type '{state.ContentType}' for task {state.TaskId}")
        };
    }
}
