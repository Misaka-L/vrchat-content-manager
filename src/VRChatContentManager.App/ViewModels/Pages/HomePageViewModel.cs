using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using Microsoft.Extensions.DependencyInjection;
using VRChatContentManager.App.Services;
using VRChatContentManager.App.ViewModels.Pages.HomeTab;

namespace VRChatContentManager.App.ViewModels.Pages;

public partial class HomePageViewModel : PageViewModelBase
{
    [ObservableProperty] public partial HomePageNavigationItem CurrentNavigationItem { get; set; }
    [ObservableProperty] public partial PageViewModelBase? CurrentPage { get; private set; }

    [ObservableProperty]
    public partial List<HomePageNavigationItem> Items { get; private set; } =
    [
        new("Tasks", MaterialIconKind.ProgressUpload, typeof(HomeTasksPageViewModel)),
        new("Contents", MaterialIconKind.CubeSend, typeof(HomeContentsPageViewModel))
    ];

    private readonly NavigationService _navigationService;
    private readonly IServiceProvider _serviceProvider;

    public HomePageViewModel(NavigationService navigationService, IServiceProvider serviceProvider)
    {
        _navigationService = navigationService;
        _serviceProvider = serviceProvider;

        PropertyChanged += (_, args) =>
        {
            if (args.PropertyName != nameof(CurrentNavigationItem))
                return;

            if (CurrentNavigationItem is null)
            {
                CurrentPage = null;
                return;
            }

            var page = (PageViewModelBase)_serviceProvider.GetRequiredService(CurrentNavigationItem.PageViewModelType)!;
            CurrentPage = page;
        };

        CurrentNavigationItem = Items[0];
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        _navigationService.Navigate<SettingsPageViewModel>();
    }
}

public record HomePageNavigationItem(string Name, MaterialIconKind Icon, Type PageViewModelType);