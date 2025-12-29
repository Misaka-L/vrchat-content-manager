using Avalonia;
using System.Runtime.Versioning;
using HotAvalonia;
using Lemon.Hosting.AvaloniauiDesktop;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Formatting.Compact;
using Serilog.Sinks.SystemConsole.Themes;
using VRChatContentPublisher.App.Extensions;
using VRChatContentPublisher.Core.Extensions;
using VRChatContentPublisher.Core.Services.App;
using VRChatContentPublisher.IpcCore;
using VRChatContentPublisher.IpcCore.Exceptions;
using VRChatContentPublisher.IpcCore.Extensions;
using VRChatContentPublisher.IpcCore.Models;

namespace VRChatContentPublisher.App;

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

        var jsonLogPath = Path.Combine(AppStorageService.GetLogsPath(), "log-.json");
        var plainTextLogPath = Path.Combine(AppStorageService.GetLogsPath(), "log-.log");
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Console(applyThemeToRedirectedOutput: true, theme: AnsiConsoleTheme.Code)
            .WriteTo.Async(writer =>
                writer.File(new CompactJsonFormatter(), jsonLogPath,
                    rollingInterval: RollingInterval.Day))
            .WriteTo.Async(writer =>
                writer.File(plainTextLogPath, rollingInterval: RollingInterval.Day))
            .WriteTo.Debug()
            .CreateLogger();

        try
        {
            using var appMutex = new AppMutex();

            try
            {
                appMutex.OwnMutex();
            }
            catch (AbandonedMutexException ex)
            {
                Log.Warning(ex,
                    "The previous instance of the application did not release the mutex properly. " +
                    "Continuing to run this instance.");
            }

            builder.Services.AddSerilog();

            builder.UseAppCore();
            builder.Services.AddAppServices();
            builder.Services.AddIpcCore();
            builder.Services.AddAvaloniauiDesktopApplication<App>(appBuilder => appBuilder
                .UseHotReload()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace());

            using var app = builder.Build();

            app.RunAvaloniauiApplication(args);
        }
        catch (MutexOwnedByAnotherInstanceException)
        {
            Log.Information("Another instance is already running. Exiting this instance.");
            Environment.ExitCode = -1;

            try
            {
                var ipcClient = new IpcClient();
                ipcClient.SendIpcCommand(IpcCommand.ActivateWindow);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to send IPC command to the existing instance.");
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Oops, the application has crashed!");
            Environment.ExitCode = -1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UseHotReload()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}