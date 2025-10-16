using Microsoft.Extensions.DependencyInjection;
using VRChatContentManager.App.Services;
using VRChatContentManager.App.ViewModels;
using VRChatContentManager.App.ViewModels.Data;
using VRChatContentManager.App.ViewModels.Data.PublishTasks;
using VRChatContentManager.App.ViewModels.Dialogs;
using VRChatContentManager.App.ViewModels.Pages;
using VRChatContentManager.App.ViewModels.Pages.GettingStarted;
using VRChatContentManager.App.ViewModels.Pages.HomeTab;
using VRChatContentManager.ConnectCore.Services.Connect.Challenge;

namespace VRChatContentManager.App.Extensions;

public static class ServicesExtenstion
{
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.AddSingleton<AppWebImageLoader>();
        
        // Dialog
        services.AddSingleton<DialogService>();

        // Dialogs
        services.AddTransient<TwoFactorAuthDialogViewModelFactory>();
        services.AddTransient<RequestChallengeDialogViewModelFactory>();

        // ViewModels
        services.AddSingleton<MainWindowViewModel>();

        services.AddSingleton<NavigationService>();

        services.AddTransient<BootstrapPageViewModel>();
        
        services.AddTransient<HomePageViewModel>();
        services.AddTransient<SettingsPageViewModel>();
        
        // Data ViewModels
        services.AddTransient<UserSessionViewModelFactory>();
        services.AddTransient<PublishTaskViewModelFactory>();
        services.AddTransient<PublishTaskManagerViewModelFactory>();

        // HomePage Tabs
        services.AddTransient<HomeTasksPageViewModel>();
        services.AddTransient<HomeContentsPageViewModel>();

        // Getting Started Pages
        services.AddTransient<GuideWelcomePageViewModel>();
        services.AddTransient<GuideAccountPageViewModel>();
        services.AddTransient<GuideSetupUnityPageViewModel>();
        
        // Connect Core
        services.AddSingleton<IRequestChallengeService, RequestChallengeService>();

        return services;
    }
}