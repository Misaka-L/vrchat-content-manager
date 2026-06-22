using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using VRChatContentPublisher.App.Services.AppLifetime;
using VRChatContentPublisher.ConnectCore.Exceptions;

namespace VRChatContentPublisher.App.Extensions;

public static class AvaloniaHostExtension
{
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    public static IServiceCollection AddAvaloniaApplication<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        TApplication>(
        this IServiceCollection services, Func<AppBuilder, AppBuilder> appBuilderConfigure)
        where TApplication : Application
    {
        return services
            .AddSingleton<TApplication>()
            .AddSingleton(provider =>
                appBuilderConfigure(AppBuilder.Configure(provider.GetRequiredService<TApplication>)));
    }

    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    [SuppressMessage("ReSharper", "MethodSupportsCancellation")]
    public static void RunAvaloniaWaitForShutdown(this IHost host, string[] args)
    {
        var lifetime = new ClassicDesktopStyleApplicationLifetime
        {
            Args = args,
            ShutdownMode = ShutdownMode.OnExplicitShutdown
        };
        host.Services.GetRequiredService<AppBuilder>().SetupWithLifetime(lifetime);
        var appLifetimeService = host.Services.GetRequiredService<AppLifetimeService>();

        var cts = new CancellationTokenSource();

        lifetime.Startup += (_, __) => appLifetimeService.NotifyAppStartup();

        Log.Information("Host is starting...");

        var hostTask = Task.Run(async () =>
        {
            try
            {
                await host.StartAsync(cts.Token);
                Log.Information("Host start completed.");
                appLifetimeService.NotifyHostStarted();
            }
            catch (OperationCanceledException)
            {
                Log.Error("Host start cancelled.");
                return;
            }
            catch (Exception ex)
            {
                // Host already do logging stuffs.
                appLifetimeService.NotifyHostStartError(ex);
                return;
            }

            try
            {
                await host.WaitForShutdownAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (!cts.IsCancellationRequested)
            {
                Log.Error("Host experienced an unexpected shutdown!");
                await appLifetimeService.WaitAppStartupNotifyHostShutdown(cts.Token);
                throw new UnexpectedHostShutdownException();
            }
        });

        lifetime.Start();

        Log.Information("Host is shutting down...");
        Task.Run(async () =>
        {
            await cts.CancelAsync();
            await hostTask;
        }).Wait();
        Log.Information("Host shutdown completed.");
    }
}