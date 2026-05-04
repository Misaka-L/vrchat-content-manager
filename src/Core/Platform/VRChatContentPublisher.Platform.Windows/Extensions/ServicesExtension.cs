using Microsoft.Extensions.DependencyInjection;
using VRChatContentPublisher.Platform.Abstraction.Services;
using VRChatContentPublisher.Platform.Windows.Services;

namespace VRChatContentPublisher.Platform.Windows.Extensions;

public static class ServicesExtension
{
    public static IServiceCollection AddWindowsPlatformServices(this IServiceCollection services)
    {
        services.AddSingleton<WindowsDesktopNotificationService>();
        services.AddSingleton<IDesktopNotificationService>(s =>
            s.GetRequiredService<WindowsDesktopNotificationService>());

        services.AddSingleton<IUpdateInstallationService, WindowsUpdateInstallationService>();

        return services;
    }
}