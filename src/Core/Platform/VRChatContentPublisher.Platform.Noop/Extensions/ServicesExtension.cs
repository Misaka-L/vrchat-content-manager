using Microsoft.Extensions.DependencyInjection;
using VRChatContentPublisher.Platform.Abstraction.Services;
using VRChatContentPublisher.Platform.Noop.Services;

namespace VRChatContentPublisher.Platform.Noop.Extensions;

public static class ServicesExtension
{
    public static IServiceCollection AddNoopPlatformServices(this IServiceCollection services)
    {
        services.AddSingleton<IDesktopNotificationService, NoopDesktopNotificationService>();
        services.AddSingleton<IUpdateInstallationService, NoopUpdateInstallationService>();

        return services;
    }
}