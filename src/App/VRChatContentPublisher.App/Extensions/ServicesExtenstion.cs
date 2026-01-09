using Microsoft.Extensions.DependencyInjection;
using VRChatContentPublisher.App.Services;
using VRChatContentPublisher.App.ViewModels;
using VRChatContentPublisher.App.ViewModels.Data;
using VRChatContentPublisher.App.ViewModels.Data.Connect;
using VRChatContentPublisher.App.ViewModels.Data.PublishTasks;
using VRChatContentPublisher.App.ViewModels.Dialogs;
using VRChatContentPublisher.App.ViewModels.NetworkDiagnostic;
using VRChatContentPublisher.App.ViewModels.Pages;
using VRChatContentPublisher.App.ViewModels.Pages.GettingStarted;
using VRChatContentPublisher.App.ViewModels.Pages.HomeTab;
using VRChatContentPublisher.App.ViewModels.Pages.Settings;
using VRChatContentPublisher.App.ViewModels.Settings;
using VRChatContentPublisher.ConnectCore.Services.Connect.Challenge;
using VRChatContentPublisher.IpcCore.Services;

namespace VRChatContentPublisher.App.Extensions;

public static class ServicesExtenstion
{
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.AddSingleton<AppWebImageLoader>();

        services.AddSingleton<AppWindowService>();
        services.AddSingleton<IActivateWindowService>(s => s.GetRequiredService<AppWindowService>());

        // Dialog
        services.AddSingleton<DialogService>();

        // Dialogs
        services.AddTransient<TwoFactorAuthDialogViewModelFactory>();
        services.AddTransient<RequestChallengeDialogViewModelFactory>();

        // ViewModels
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<TaskErrorReportWindowViewModel>();
        services.AddTransient<NetworkDiagnosticWindowViewModel>();

        services.AddSingleton<NavigationService>();

        services.AddTransient<BootstrapPageViewModel>();

        services.AddTransient<HomePageViewModel>();
        services.AddTransient<SettingsPageViewModel>();

        // Data ViewModels
        services.AddTransient<UserSessionViewModelFactory>();
        services.AddTransient<PublishTaskViewModelFactory>();
        services.AddTransient<PublishTaskManagerViewModelFactory>();
        services.AddTransient<InvalidSessionTaskManagerViewModelFactory>();
        services.AddTransient<PublishTaskManagerContainerViewModelFactory>();

        services.AddTransient<RpcClientSessionViewModelFactory>();

        // HomePage Tabs
        services.AddTransient<HomeTasksPageViewModel>();

        // Getting Started Pages
        services.AddTransient<GuideWelcomePageViewModel>();
        services.AddTransient<GuideSetupUnityPageViewModel>();
        services.AddTransient<GuideOpenConnectSettingsPageViewModel>();
        services.AddTransient<GuideConnectUnityPageViewModel>();

        // Settings Pages
        services.AddTransient<AddAccountPageViewModelFactory>();
        services.AddTransient<SettingsFixAccountPageViewModelFactory>();
        services.AddTransient<LicensePageViewModel>();

        // Settings Sections
        services.AddTransient<AccountsSettingsViewModel>();
        services.AddTransient<ConnectSettingsViewModel>();
        services.AddTransient<HttpProxySettingsViewModel>();
        services.AddTransient<SessionsSettingsViewModel>();
        services.AddTransient<AboutSettingsViewModel>();
        services.AddTransient<DebugSettingsViewModel>();

        // Connect Core
        services.AddSingleton<IRequestChallengeService, RequestChallengeService>();

        return services;
    }
}