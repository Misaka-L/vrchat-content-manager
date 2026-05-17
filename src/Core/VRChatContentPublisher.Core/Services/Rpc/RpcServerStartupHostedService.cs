using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VRChatContentPublisher.ConnectCore.Extensions;
using VRChatContentPublisher.ConnectCore.Services.Connect;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;

namespace VRChatContentPublisher.Core.Services.Rpc;

public sealed class RpcServerStartupHostedService(
    HttpServerService httpServerService,
    EndpointService endpointService,
    IWritableOptions<AppSettings> appSettings,
    RpcStartupPortWarningState startupPortWarningState,
    ILogger<RpcServerStartupHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        endpointService.MapConnectService();

        var configuredPort = appSettings.Value.RpcServerPort;
        var wasConfiguredPortOutOfRange = false;
        if (configuredPort is < HttpServerService.MinUserPort or > HttpServerService.MaxUserPort)
        {
            wasConfiguredPortOutOfRange = true;
            logger.LogWarning(
                "Configured RPC port {RpcPort} is out of range. Falling back to default port {DefaultPort}.",
                configuredPort,
                HttpServerService.DefaultPort);
            configuredPort = HttpServerService.DefaultPort;
        }

        if (await httpServerService.TryStartOnPortAsync(configuredPort, cancellationToken))
        {
            if (appSettings.Value.RpcServerPort != configuredPort)
            {
                await appSettings.UpdateAsync(settings => { settings.RpcServerPort = configuredPort; });
            }

            return;
        }

        var configuredPortIsInUse = !wasConfiguredPortOutOfRange && httpServerService.IsPortInUse(configuredPort);

        var fallbackPort = httpServerService.FindAvailablePort(configuredPort);
        if (fallbackPort is null)
        {
            logger.LogWarning(
                "Failed to start RPC HTTP server. No free port was found in range {MinPort}-{MaxPort}.",
                HttpServerService.MinUserPort,
                HttpServerService.MaxUserPort);
            throw new InvalidOperationException("No free RPC port found in the allowed range.");
        }

        if (!await httpServerService.TryStartOnPortAsync(fallbackPort.Value, cancellationToken))
        {
            logger.LogWarning("Failed to start RPC HTTP server on fallback port {RpcPort}.", fallbackPort.Value);
            throw new InvalidOperationException("Unable to start RPC server on the fallback port.");
        }

        await appSettings.UpdateAsync(settings => { settings.RpcServerPort = fallbackPort.Value; });

        if (configuredPortIsInUse)
        {
            startupPortWarningState.Set(configuredPort, fallbackPort.Value);
        }

        logger.LogWarning(
            "Configured RPC port {ConfiguredPort} was unavailable. Started RPC server on fallback port {FallbackPort}.",
            configuredPort,
            fallbackPort.Value);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await httpServerService.StopAsync(cancellationToken);
    }
}