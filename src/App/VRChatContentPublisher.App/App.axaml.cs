using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using VRChatContentPublisher.App.Dialogs;
using VRChatContentPublisher.App.Pages;
using VRChatContentPublisher.App.Pages.GettingStarted;
using VRChatContentPublisher.App.Pages.HomeTab;
using VRChatContentPublisher.App.Pages.Settings;
using VRChatContentPublisher.App.Services;
using VRChatContentPublisher.App.ViewModels;
using VRChatContentPublisher.App.ViewModels.Data;
using VRChatContentPublisher.App.ViewModels.Data.Connect;
using VRChatContentPublisher.App.ViewModels.Data.PublishTasks;
using VRChatContentPublisher.App.ViewModels.Dialogs;
using VRChatContentPublisher.App.ViewModels.Pages;
using VRChatContentPublisher.App.ViewModels.Pages.GettingStarted;
using VRChatContentPublisher.App.ViewModels.Pages.HomeTab;
using VRChatContentPublisher.App.ViewModels.Pages.Settings;
using VRChatContentPublisher.App.ViewModels.Settings;
using VRChatContentPublisher.App.Views;
using VRChatContentPublisher.App.Views.Data.Connect;
using VRChatContentPublisher.App.Views.Data.PublishTasks;
using VRChatContentPublisher.App.Views.Data.Settings;
using VRChatContentPublisher.App.Views.Settings;
using VRChatContentPublisher.Core;
using VRChatContentPublisher.Core.Services.App;

namespace VRChatContentPublisher.App;

public partial class App : Application
{
#pragma warning disable CS8600
#pragma warning disable CS8603
    public new static App Current => (App)Application.Current;
#pragma warning restore CS8603
#pragma warning restore CS8600

    private readonly IServiceProvider _serviceProvider = null!;

    public readonly AppWebImageLoader AsyncImageLoader;

    public App()
    {
        // Make Previewer happy
        var httpClient = new HttpClient();
        httpClient.AddUserAgent();

        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        AsyncImageLoader = new AppWebImageLoader(new RemoteImageService(httpClient, memoryCache), memoryCache);
    }

    public App(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        AsyncImageLoader = _serviceProvider.GetRequiredService<AppWebImageLoader>();
    }

    public override void Initialize()
    {
        ViewLocator.Register<BootstrapPageViewModel, BootstrapPage>();

        ViewLocator.Register<HomePageViewModel, HomePage>();
        ViewLocator.Register<SettingsPageViewModel, SettingsPage>();

        // HomePage Tabs
        ViewLocator.Register<HomeTasksPageViewModel, HomeTasksPage>();

        // Getting Started Pages
        ViewLocator.Register<GuideWelcomePageViewModel, GuideWelcomePage>();
        ViewLocator.Register<GuideSetupUnityPageViewModel, GuideSetupUnityPage>();
        ViewLocator.Register<GuideOpenConnectSettingsPageViewModel, GuideOpenConnectSettingsPage>();
        ViewLocator.Register<GuideConnectUnityPageViewModel, GuideConnectUnityPage>();

        // Settings Pages
        ViewLocator.Register<SettingsFixAccountPageViewModel, SettingsFixAccountPage>();
        ViewLocator.Register<AddAccountPageViewModel, AddAccountPage>();
        ViewLocator.Register<LicensePageViewModel, LicensePage>();

        // Dialogs
        ViewLocator.Register<TwoFactorAuthDialogViewModel, TwoFactorAuthDialog>();
        ViewLocator.Register<RequestChallengeDialogViewModel, RequestChallengeDialog>();
        ViewLocator.Register<ExitAppDialogViewModel, ExitAppDialog>();

        // Data
        ViewLocator.Register<PublishTaskManagerViewModel, PublishTaskManagerView>();
        ViewLocator.Register<InvalidSessionTaskManagerViewModel, InvalidSessionTaskManagerView>();
        ViewLocator.Register<PublishTaskViewModel, PublishTaskView>();
        ViewLocator.Register<PublishTaskManagerContainerViewModel, PublishTaskManagerContainerView>();

        ViewLocator.Register<RpcClientSessionViewModel, RpcClientSessionView>();
        ViewLocator.Register<UserSessionViewModel, UserSessionView>();

        // Settings Section
        ViewLocator.Register<ConnectSettingsViewModel, ConnectSettingsView>();
        ViewLocator.Register<HttpProxySettingsViewModel, HttpProxySettingsView>();
        ViewLocator.Register<AccountsSettingsViewModel, AccountsSettingsView>();
        ViewLocator.Register<SessionsSettingsViewModel, SessionsSettingsView>();
        ViewLocator.Register<AboutSettingsViewModel, AboutSettingsView>();
        ViewLocator.Register<DebugSettingsViewModel, DebugSettingsView>();

        AvaloniaXamlLoader.Load(this);

        this.AttachDeveloperTools();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = new MainWindow
            {
                DataContext = _serviceProvider.GetRequiredService<MainWindowViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }

    private void ShowWindowClicked(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;

        desktop.MainWindow?.Show();
        desktop.MainWindow?.Activate();
    }

    private async void ExitAppClicked(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;

        var dialogService = _serviceProvider.GetRequiredService<DialogService>();
        var exitAppDialogViewModel = _serviceProvider.GetRequiredService<ExitAppDialogViewModel>();

        desktop.MainWindow?.Show();
        desktop.MainWindow?.Activate();

        if (await dialogService.ShowDialogAsync(exitAppDialogViewModel) is not true)
            return;

        Dispatcher.UIThread.InvokeShutdown();
    }

    private void OpenLogsFolderClicked(object? sender, EventArgs e)
    {
        var directoryPath = AppStorageService.GetLogsPath();
        if (!Directory.Exists(directoryPath))
            return;

        var directoryInfo = new DirectoryInfo(directoryPath);

        var topLevel =
            TopLevel.GetTopLevel((ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow);
        if (topLevel?.Launcher is { } launcher)
        {
            // Fire and forget
            _ = launcher.LaunchDirectoryInfoAsync(directoryInfo);
        }
    }
}