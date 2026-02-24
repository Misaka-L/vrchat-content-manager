using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using Microsoft.Extensions.DependencyInjection;
using VRChatContentPublisher.App.Services;
using VRChatContentPublisher.App.ViewModels.Pages.HomeTab;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;

namespace VRChatContentPublisher.App.ViewModels.Pages;

public partial class HomePageViewModel : PageViewModelBase
{
    public bool UseRgbCyclingBackgroundMenu => _appSettings.Value.UseRgbCyclingBackgroundMenu;

    [ObservableProperty] public partial HomePageNavigationItem CurrentNavigationItem { get; set; }
    [ObservableProperty] public partial PageViewModelBase? CurrentPage { get; private set; }

    [ObservableProperty]
    public partial List<HomePageNavigationItem> Items { get; private set; } =
    [
        new("Tasks", MaterialIconKind.ProgressUpload, typeof(HomeTasksPageViewModel))
    ];

    public bool IsPinned => _appWindowService.IsPinned();

    private readonly NavigationService _navigationService;
    private readonly AppWindowService _appWindowService;
    private readonly IWritableOptions<AppSettings> _appSettings;
    private readonly IServiceProvider _serviceProvider;

    public HomePageViewModel(
        NavigationService navigationService,
        IServiceProvider serviceProvider,
        IWritableOptions<AppSettings> appSettings, AppWindowService appWindowService)
    {
        _navigationService = navigationService;
        _serviceProvider = serviceProvider;
        _appSettings = appSettings;
        _appWindowService = appWindowService;

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
    private async Task Load()
    {
        await _appSettings.UpdateAsync(settings => { settings.SkipFirstSetup = true; });
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        _navigationService.Navigate<SettingsPageViewModel>();
    }

    [RelayCommand]
    private void ToggleWindowPin()
    {
        _appWindowService.SetPin(!_appWindowService.IsPinned());
        OnPropertyChanged(nameof(IsPinned));
    }
}

public record HomePageNavigationItem(string Name, MaterialIconKind Icon, Type PageViewModelType);