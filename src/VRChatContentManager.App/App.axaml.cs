using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using VRChatContentManager.App.Dialogs;
using VRChatContentManager.App.Pages;
using VRChatContentManager.App.Pages.GettingStarted;
using VRChatContentManager.App.Pages.HomeTab;
using VRChatContentManager.App.ViewModels;
using VRChatContentManager.App.ViewModels.Data.PublishTasks;
using VRChatContentManager.App.ViewModels.Dialogs;
using VRChatContentManager.App.ViewModels.Pages;
using VRChatContentManager.App.ViewModels.Pages.GettingStarted;
using VRChatContentManager.App.ViewModels.Pages.HomeTab;
using VRChatContentManager.App.Views;
using VRChatContentManager.App.Views.Data;
using VRChatContentManager.App.Views.Data.PublishTasks;
using VRChatContentManager.Core;
using VRChatContentManager.Core.Services.App;

namespace VRChatContentManager.App;

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
        ViewLocator.Register<HomeContentsPageViewModel, HomeContentsPage>();
        
        // Getting Started Pages
        ViewLocator.Register<GuideWelcomePageViewModel, GuideWelcomePage>();
        ViewLocator.Register<GuideAccountPageViewModel, GuideAccountPage>();
        ViewLocator.Register<GuideSetupUnityPageViewModel, GuideSetupUnityPage>();
        ViewLocator.Register<GuideFinishSetupPageViewModel, GuideFinishPage>();
        
        // Dialogs
        ViewLocator.Register<TwoFactorAuthDialogViewModel, TwoFactorAuthDialog>();
        ViewLocator.Register<RequestChallengeDialogViewModel, RequestChallengeDialog>();
        
        // Data
        ViewLocator.Register<PublishTaskManagerViewModel, PublishTaskManagerView>();
        ViewLocator.Register<PublishTaskViewModel, PublishTaskView>();
        
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
}