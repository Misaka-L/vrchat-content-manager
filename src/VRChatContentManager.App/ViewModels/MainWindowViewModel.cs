using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using VRChatContentManager.App.Pages.GettingStarted;
using VRChatContentManager.App.Services;
using VRChatContentManager.App.ViewModels.Pages;
using VRChatContentManager.App.ViewModels.Pages.GettingStarted;
using VRChatContentManager.Core.Settings;
using VRChatContentManager.Core.Settings.Models;

namespace VRChatContentManager.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, INavigationHost
{
    [ObservableProperty] public partial PageViewModelBase? CurrentPage { get; private set; }

    private readonly NavigationService _navigationService;
    private readonly DialogService _dialogService;

    private readonly IWritableOptions<AppSettings> _appSettings;

    public string DialogHostId { get; } = "MainWindow-" + Guid.NewGuid().ToString("D");

    public MainWindowViewModel(NavigationService navigationService, DialogService dialogService,
        IWritableOptions<AppSettings> appSettings)
    {
        _navigationService = navigationService;
        _dialogService = dialogService;
        _appSettings = appSettings;

        _dialogService.SetDialogHostId(DialogHostId);

        _navigationService.Register(this);

        if (!_appSettings.Value.SkipFirstSetup)
        {
            _navigationService.Navigate<GuideWelcomePageViewModel>();
            return;
        }

        _navigationService.Navigate<HomePageViewModel>();
    }

    public void Navigate(PageViewModelBase pageViewModel)
    {
        CurrentPage = pageViewModel;
    }
}