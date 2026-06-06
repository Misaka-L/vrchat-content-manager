using Microsoft.Extensions.DependencyInjection;
using VRChatContentPublisher.VRChatApi.ApiClient;
using VRChatContentPublisher.VRChatApi.Models;
using VRChatContentPublisher.VRChatApi.Services;

namespace VRChatContentPublisher.VRChatApi.Extensions;

public static class ServicesExtension
{
    public static IServiceCollection AddVRChatApi(
        this IServiceCollection services,
        Action<VRChatApiOptions> configureOptions)
    {
        services.Configure(configureOptions);

        services.AddSingleton<AppResiliencePipelineBuilderFactory>();

        services.AddTransient<ConcurrentMultipartUploaderFactory>();
        services.AddTransient<VRChatApiClientFactory>();
        services.AddTransient<VRChatApiDiagnosticService>();

        return services;
    }
}