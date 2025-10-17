﻿using Avalonia;
using System;
using System.IO;
using System.Runtime.Versioning;
using HotAvalonia;
using Lemon.Hosting.AvaloniauiDesktop;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Formatting.Compact;
using Serilog.Sinks.SystemConsole.Themes;
using VRChatContentManager.App.Extensions;
using VRChatContentManager.Core.Extensions;
using VRChatContentManager.Core.Services.App;

namespace VRChatContentManager.App;

internal sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    public static void Main(string[] args)
    {
        var builder = new HostApplicationBuilder();

        var logPath = Path.Combine(AppStorageService.GetStoragePath(), "logs", "log-.json");
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Console(applyThemeToRedirectedOutput: true, theme: AnsiConsoleTheme.Code)
            .WriteTo.Async(writer =>
                writer.File(new CompactJsonFormatter(), logPath,
                    rollingInterval: RollingInterval.Day))
            .WriteTo.Debug()
            .CreateLogger();

        builder.Services.AddSerilog();

        builder.UseAppCore();
        builder.Services.AddAppServices();
        builder.Services.AddAvaloniauiDesktopApplication<App>(appBuilder => appBuilder
            .UseHotReload()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace());

        var app = builder.Build();

        app.RunAvaloniauiApplication(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UseHotReload()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}