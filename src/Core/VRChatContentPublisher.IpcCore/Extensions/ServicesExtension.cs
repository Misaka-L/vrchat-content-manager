using Microsoft.Extensions.DependencyInjection;
using VRChatContentPublisher.IpcCore.Services;

namespace VRChatContentPublisher.IpcCore.Extensions;

public static class ServicesExtension
{
    public static IServiceCollection AddIpcCore(this IServiceCollection services)
    {
        services.AddSingleton<NamedPipeService>();
        services.AddSingleton<IpcCommandService>();

        services.AddHostedService<IpcHostedService>();

        return services;
    }
}