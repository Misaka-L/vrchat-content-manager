using Microsoft.Extensions.DependencyInjection;
using VRChatContentManager.ConnectCore.Middlewares;
using VRChatContentManager.ConnectCore.Services;

namespace VRChatContentManager.ConnectCore.Extensions;

public static class ServicesExtenstion
{
    public static IServiceCollection AddConnectCore(this IServiceCollection services)
    {
        services.AddSingleton<HttpServerService>();
        services.AddHostedService<ConnectHostService>();
        
        // Middlewares
        services.AddTransient<RequestLoggingMiddleware>();
        services.AddTransient<EndpointMiddleware>();
        services.AddTransient<PostRequestLoggingMiddleware>();
        
        return services;
    }
}