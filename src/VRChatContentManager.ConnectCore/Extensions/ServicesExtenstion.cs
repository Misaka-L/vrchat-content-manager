using Microsoft.Extensions.DependencyInjection;
using VRChatContentManager.ConnectCore.Middlewares;
using VRChatContentManager.ConnectCore.Services;
using VRChatContentManager.ConnectCore.Services.Connect;

namespace VRChatContentManager.ConnectCore.Extensions;

public static class ServicesExtenstion
{
    public static IServiceCollection AddConnectCore(this IServiceCollection services)
    {
        services.AddSingleton<ClientSessionService>();
        
        services.AddSingleton<HttpServerService>();
        services.AddHostedService<ConnectHostService>();
        services.AddSingleton<EndpointService>();
        
        // Middlewares
        services.AddTransient<RequestLoggingMiddleware>();
        services.AddTransient<JwtAuthMiddleware>();
        services.AddTransient<EndpointMiddleware>();
        services.AddTransient<PostRequestLoggingMiddleware>();
        
        return services;
    }
}