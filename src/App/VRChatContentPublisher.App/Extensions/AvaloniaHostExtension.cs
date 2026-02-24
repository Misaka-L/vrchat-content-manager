using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

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

        var cts = new CancellationTokenSource();

        Log.Information("Host is starting...");
        _ = host.StartAsync(cts.Token).ContinueWith(_ => Log.Information("Host start completed."));
        Log.Information("Host start cancelled.");

        lifetime.Start();

        Log.Information("Host is shutting down...");
        Task.Run(async () =>
        {
            await cts.CancelAsync();
            await host.StopAsync();
        }).Wait();
        Log.Information("Host shutdown completed.");
    }
}