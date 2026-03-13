using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Sockets;
using VRChatContentPublisher.ConnectCore.Extensions;
using VRChatContentPublisher.ConnectCore.Middlewares;
using VRChatContentPublisher.ConnectCore.Models.Api.V1;

namespace VRChatContentPublisher.ConnectCore.Services.Connect;

public sealed class HttpServerService
{
    public const int DefaultPort = 59328;
    public const int MinUserPort = 1024;
    public const int MaxUserPort = 65535;

    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<HttpServerService> _logger;

    private KestrelServer? _kestrelServer;
    private readonly SimpleHttpApplication _simpleHttpApplication;

    public int? CurrentPort { get; private set; }

    private readonly List<MiddlewareBase> _preRequestMiddlewares = [];
    private readonly List<MiddlewareBase> _postRequestMiddlewares = [];

    public HttpServerService(ILoggerFactory loggerFactory, ILogger<HttpServerService> logger,
        EndpointMiddleware endpointMiddleware,
        PostRequestLoggingMiddleware postRequestLoggingMiddleware, JwtAuthMiddleware jwtAuthMiddleware)
    {
        _loggerFactory = loggerFactory;
        _logger = logger;
        _simpleHttpApplication = new SimpleHttpApplication(HandleRequestAsync);

        _preRequestMiddlewares.Add(jwtAuthMiddleware);

        _postRequestMiddlewares.Add(endpointMiddleware);
        _postRequestMiddlewares.Add(postRequestLoggingMiddleware);
    }

    public async Task<bool> TryStartOnPortAsync(int port, CancellationToken cancellationToken)
    {
        try
        {
            await StartAsync(port, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to start RPC HTTP server on port {RpcPort}.", port);
            return false;
        }
    }

    public async Task StartAsync(int port, CancellationToken cancellationToken)
    {
        if (!IsValidUserPort(port))
            throw new ArgumentOutOfRangeException(nameof(port),
                $"Port must be between {MinUserPort} and {MaxUserPort}.");

        if (_kestrelServer is not null)
            throw new InvalidOperationException("HTTP server is already started.");

        var server = CreateServer(port);

        try
        {
            await server.StartAsync(_simpleHttpApplication, cancellationToken);
            _kestrelServer = server;
            CurrentPort = port;
            _logger.LogInformation("HTTP server started on port {RpcPort}.", port);
        }
        catch
        {
            await SafeStopAsync(server, cancellationToken);
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_kestrelServer is null)
            return;

        await _kestrelServer.StopAsync(cancellationToken);
        _kestrelServer = null;
        CurrentPort = null;
        _logger.LogInformation("HTTP server stopped.");
    }

    public async Task<(bool IsSuccess, string? ErrorMessage)> RebindAsync(int targetPort,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidUserPort(targetPort))
            return (false, $"Port must be between {MinUserPort} and {MaxUserPort}.");

        if (_kestrelServer is null || CurrentPort is null)
            return (false, "RPC server is not running.");

        if (CurrentPort.Value == targetPort)
            return (true, null);

        var previousServer = _kestrelServer;
        var previousPort = CurrentPort.Value;
        var replacementServer = CreateServer(targetPort);

        try
        {
            await replacementServer.StartAsync(_simpleHttpApplication, cancellationToken);
        }
        catch (Exception ex)
        {
            await SafeStopAsync(replacementServer, cancellationToken);
            _logger.LogWarning(ex, "Failed to switch RPC HTTP server to port {RpcPort}.", targetPort);
            return (false,
                $"Unable to use port {targetPort}. It may already be in use. Please choose another port.");
        }

        try
        {
            await previousServer.StopAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await SafeStopAsync(replacementServer, cancellationToken);
            _logger.LogWarning(ex,
                "Failed to stop previous RPC HTTP server on port {RpcPort} while switching ports.", previousPort);
            return (false, "Failed to switch RPC server port safely. The previous port remains active.");
        }

        _kestrelServer = replacementServer;
        CurrentPort = targetPort;

        _logger.LogInformation("RPC HTTP server port changed from {PreviousPort} to {CurrentPort}.", previousPort,
            targetPort);
        return (true, null);
    }

    public int? FindAvailablePort(int preferredPort)
    {
        var startPort = IsValidUserPort(preferredPort) ? preferredPort : DefaultPort;
        var candidate = startPort;
        var totalPorts = MaxUserPort - MinUserPort + 1;

        for (var i = 0; i < totalPorts; i++)
        {
            if (CurrentPort == candidate)
                return candidate;

            if (IsPortAvailable(candidate))
                return candidate;

            candidate = candidate == MaxUserPort ? MinUserPort : candidate + 1;
        }

        return null;
    }

    public bool IsPortInUse(int port)
    {
        if (!IsValidUserPort(port))
            throw new ArgumentOutOfRangeException(nameof(port),
                $"Port must be between {MinUserPort} and {MaxUserPort}.");

        return !IsPortAvailable(port);
    }

    private static bool IsPortAvailable(int port)
    {
        try
        {
            using var tcpListener = new TcpListener(IPAddress.Loopback, port);
            tcpListener.Start();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    private KestrelServer CreateServer(int port)
    {
        var kestrelServerOptions = new KestrelServerOptions();

        kestrelServerOptions.Limits.MaxRequestBodySize = null;
        kestrelServerOptions.ListenLocalhost(port);

        var transportOptions = new SocketTransportOptions();
        var transportFactory = new SocketTransportFactory(
            new OptionsWrapper<SocketTransportOptions>(transportOptions), _loggerFactory);

        return new KestrelServer(
            new OptionsWrapper<KestrelServerOptions>(kestrelServerOptions), transportFactory, _loggerFactory);
    }

    private static bool IsValidUserPort(int port)
    {
        return port is >= MinUserPort and <= MaxUserPort;
    }

    private static async Task SafeStopAsync(KestrelServer server, CancellationToken cancellationToken)
    {
        try
        {
            await server.StopAsync(cancellationToken);
        }
        catch
        {
            // ignore cleanup failures from partially-started servers
        }
    }

    private async Task HandleRequestAsync(HttpContext httpContext)
    {
        var requestId = httpContext.TraceIdentifier;
        httpContext.Response.Headers.Append("X-Request-Id", requestId);

        using (_logger.BeginScope(
                   "{RequestId} {RpcClientIp}:{RpcClientPort} {RpcHttpMethod} {RpcHttpPath}{RpcHttpQuery}",
                   requestId,
                   httpContext.Connection.RemoteIpAddress,
                   httpContext.Connection.RemotePort,
                   httpContext.Request.Method,
                   httpContext.Request.Path,
                   httpContext.Request.QueryString))
        {
            _logger.LogInformation(
                "{RequestId} {RpcClientIp}:{RpcClientPort} {RpcHttpMethod} {RpcHttpPath}{RpcHttpQuery}",
                requestId,
                httpContext.Connection.RemoteIpAddress,
                httpContext.Connection.RemotePort,
                httpContext.Request.Method,
                httpContext.Request.Path,
                httpContext.Request.QueryString);

            try
            {
                var middlewares = new List<MiddlewareBase>();
                middlewares.AddRange(_preRequestMiddlewares);
                middlewares.AddRange(_postRequestMiddlewares);

                await RunMiddlewaresAsync(httpContext, middlewares);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling HTTP request");
                await httpContext.Response.WriteProblemAsync(ApiV1ProblemType.Undocumented,
                    StatusCodes.Status500InternalServerError,
                    "Internal Server Error", "An unexpected error occurred.");
            }
        }
    }

    private async Task RunMiddlewaresAsync(HttpContext httpContext, List<MiddlewareBase> middlewares)
    {
        var index = 0;
        await Next();
        return;

        async Task Next()
        {
            if (index < middlewares.Count)
            {
                var current = middlewares[index];
                index++;
                await current.ExecuteAsync(httpContext, Next);
            }
        }
    }
}