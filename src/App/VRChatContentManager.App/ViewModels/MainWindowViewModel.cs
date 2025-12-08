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

public partial class MainWindowViewModel : ViewModelBase, INavigationHost, IAppWindow
{
    [ObservableProperty] public partial PageViewModelBase? CurrentPage { get; private set; }

    [ObservableProperty] public partial bool Pinned { get; private set; }

    private readonly NavigationService _navigationService;
    private readonly DialogService _dialogService;

    public string DialogHostId { get; } = "MainWindow-" + Guid.NewGuid().ToString("D");

    public MainWindowViewModel(NavigationService navigationService, DialogService dialogService,
        AppWindowService appWindowService)
    {
        _navigationService = navigationService;
        _dialogService = dialogService;

        _dialogService.SetDialogHostId(DialogHostId);

        _navigationService.Register(this);

        _navigationService.Navigate<BootstrapPageViewModel>();
        
        appWindowService.Register(this);
    }

    public void Navigate(PageViewModelBase pageViewModel)
    {
        CurrentPage = pageViewModel;
    }

    public void SetPin(bool isPinned) => Pinned = isPinned;
    public bool IsPinned() => Pinned;
}