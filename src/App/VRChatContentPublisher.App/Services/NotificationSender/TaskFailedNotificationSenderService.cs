using MessagePipe;
using Microsoft.Extensions.Hosting;
using VRChatContentPublisher.Core.Events.PublishTask;
using VRChatContentPublisher.Core.Models;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;

namespace VRChatContentPublisher.App.Services.NotificationSender;

public sealed class TaskFailedNotificationSenderService(
    IWritableOptions<AppSettings> appSettings,
    AppNotificationService appNotificationService,
    ISubscriber<PublishTaskProgressChangedEvent> progressSubscriber)
    : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        progressSubscriber.Subscribe(args =>
        {
            if (args.Status != ContentPublishTaskStatus.Failed)
                return;

            if (!appSettings.Value.SendNotificationOnTaskFailed)
                return;

            var contentType = MapContentTypeLabel(args.TaskService.ContentType);
            var title = $"{contentType} \"{args.TaskService.ContentName}\" Publish failed";

            _ = appNotificationService.SendNotificationAsync(title, args.ProgressText).AsTask();
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static string MapContentTypeLabel(string contentType)
    {
        return contentType switch
        {
            "world" => "World",
            "avatar" => "Avatar",
            _ => contentType
        };
    }
}