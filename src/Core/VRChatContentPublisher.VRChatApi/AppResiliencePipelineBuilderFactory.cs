using Microsoft.Extensions.Logging;
using Polly;

namespace VRChatContentPublisher.VRChatApi;

public sealed class AppResiliencePipelineBuilderFactory(ILoggerFactory loggerFactory)
{
    public ResiliencePipelineBuilder<T> CreateBuilder<T>(string? name = null, string? instanceName = null)
    {
        return new ResiliencePipelineBuilder<T>()
            {
                Name = name,
                InstanceName = instanceName
            }
            .ConfigureTelemetry(loggerFactory);
    }
}