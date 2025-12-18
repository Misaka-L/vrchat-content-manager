using Microsoft.Extensions.DependencyInjection;
using VRChatContentManager.IpcCore.Services;

namespace VRChatContentManager.IpcCore.Extensions;

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