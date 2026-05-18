using Antelcat.I18N.Avalonia;
using MessagePipe;
using Microsoft.Extensions.Hosting;
using VRChatContentPublisher.App.Localization;
using VRChatContentPublisher.Core.ContentPublishing.PublishTask.Models;
using VRChatContentPublisher.Core.Events.PublishTask;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;

namespace VRChatContentPublisher.App.Services.Notification.Sender;

public sealed class TaskFailedNotificationSenderService(
    IWritableOptions<AppSettings> appSettings,
    DesktopNotificationService desktopNotificationService,
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
            var title = string.Format(
                I18NExtension.Translate(LangKeys.Notifications_Task_Failed_Title_Template) ??
                "Publish {0} \"{1}\" Failed",
                contentType, args.TaskService.ContentName
            );

            _ = desktopNotificationService.SendNotificationAsync(title, args.ProgressText).AsTask();
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
            "world" => I18NExtension.Translate(LangKeys.Common_Content_Type_World) ?? "World",
            "avatar" => I18NExtension.Translate(LangKeys.Common_Content_Type_Avatar) ?? "Avatar",
            _ => contentType
        };
    }
}