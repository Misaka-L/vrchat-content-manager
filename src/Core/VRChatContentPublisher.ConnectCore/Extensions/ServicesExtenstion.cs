using Microsoft.Extensions.DependencyInjection;
using VRChatContentPublisher.ConnectCore.Middlewares;
using VRChatContentPublisher.ConnectCore.Services.Connect;
using VRChatContentPublisher.ConnectCore.Services.Connect.Metadata;

namespace VRChatContentPublisher.ConnectCore.Extensions;

public static class ServicesExtenstion
{
    public static IServiceCollection AddConnectCore(this IServiceCollection services)
    {
        services.AddSingleton<ClientSessionService>();
        
        services.AddSingleton<HttpServerService>();
        services.AddHostedService<ConnectHostService>();
        services.AddSingleton<EndpointService>();

        services.AddTransient<ConnectMetadataService>();
        
        // Middlewares
        services.AddTransient<JwtAuthMiddleware>();
        services.AddTransient<EndpointMiddleware>();
        services.AddTransient<PostRequestLoggingMiddleware>();
        
        return services;
    }
}