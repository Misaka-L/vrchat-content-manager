using Microsoft.Extensions.DependencyInjection;
using VRChatContentManager.App.Services;
using VRChatContentManager.App.ViewModels;
using VRChatContentManager.App.ViewModels.Dialogs;
using VRChatContentManager.App.ViewModels.Pages;
using VRChatContentManager.App.ViewModels.Pages.GettingStarted;
using VRChatContentManager.App.ViewModels.Pages.HomeTab;

namespace VRChatContentManager.App.Extensions;

public static class ServicesExtenstion
{
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        // Dialog
        services.AddSingleton<DialogService>();

        // Dialogs
        services.AddTransient<TwoFactorAuthDialogViewModelFactory>();

        // ViewModels
        services.AddSingleton<MainWindowViewModel>();

        services.AddSingleton<NavigationService>();

        services.AddTransient<HomePageViewModel>();
        services.AddTransient<SettingsPageViewModel>();

        // HomePage Tabs
        services.AddTransient<HomeTasksPageViewModel>();
        services.AddTransient<HomeContentsPageViewModel>();

        // Getting Started Pages
        services.AddTransient<GuideWelcomePageViewModel>();
        services.AddTransient<GuideAccountPageViewModel>();
        services.AddTransient<GuideSetupUnityPageViewModel>();

        return services;
    }
}