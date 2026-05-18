using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using Microsoft.Extensions.DependencyInjection;
using VRChatContentPublisher.App.Localization;
using VRChatContentPublisher.App.Services;
using VRChatContentPublisher.App.Services.Dialog;
using VRChatContentPublisher.App.ViewModels.Dialogs;
using VRChatContentPublisher.App.ViewModels.Pages.HomeTab;
using VRChatContentPublisher.Core.Rpc;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;

namespace VRChatContentPublisher.App.ViewModels.Pages;

public partial class HomePageViewModel : PageViewModelBase
{
    public bool UseRgbCyclingBackgroundMenu => _appSettings.Value.UseRgbCyclingBackgroundMenu;

    [ObservableProperty] public partial HomePageNavigationItem? CurrentNavigationItem { get; set; }
    [ObservableProperty] public partial PageViewModelBase? CurrentPage { get; private set; }

    public AvaloniaList<HomePageNavigationItem> Items { get; } =
    [
        new(LangKeys.Pages_Tasks_Title, MaterialIconKind.ProgressUpload, typeof(HomeTasksPageViewModel))
    ];

    public bool IsPinned => _appWindowService.IsPinned();
    public bool IsBorderless => _appWindowService.IsBorderless();

    private readonly NavigationService _navigationService;
    private readonly AppWindowService _appWindowService;
    private readonly DialogService _dialogService;
    private readonly RpcStartupPortWarningState _startupPortWarningState;
    private readonly StartupPortChangedDialogViewModelFactory _startupPortChangedDialogFactory;
    private readonly IWritableOptions<AppSettings> _appSettings;

    public HomePageViewModel(
        NavigationService navigationService,
        DialogService dialogService,
        IServiceProvider serviceProvider,
        IWritableOptions<AppSettings> appSettings,
        AppWindowService appWindowService,
        RpcStartupPortWarningState startupPortWarningState,
        StartupPortChangedDialogViewModelFactory startupPortChangedDialogFactory)
    {
        _navigationService = navigationService;
        _dialogService = dialogService;
        _appSettings = appSettings;
        _appWindowService = appWindowService;
        _startupPortWarningState = startupPortWarningState;
        _startupPortChangedDialogFactory = startupPortChangedDialogFactory;

        PropertyChanged += (_, args) =>
        {
            if (args.PropertyName != nameof(CurrentNavigationItem))
                return;

            if (CurrentNavigationItem is null)
            {
                CurrentPage = null;
                return;
            }

            var page = (PageViewModelBase)serviceProvider.GetRequiredService(CurrentNavigationItem.PageViewModelType);
            CurrentPage = page;
        };

        CurrentNavigationItem = Items[0];
    }

    [RelayCommand]
    private async Task Load()
    {
        _appWindowService.IsBorderlessChanged += OnIsBorderlessChanged;
        _appWindowService.IsPinnedChanged += OnIsPinnedChanged;

        await _appSettings.UpdateAsync(settings => { settings.SkipFirstSetup = true; });

        // Consume before showing so the warning is guaranteed to be launch-only.
        if (!_startupPortWarningState.TryConsume(out var warning))
            return;

        await _dialogService.ShowDialogAsync(
            _startupPortChangedDialogFactory.Create(warning.ConfiguredPort, warning.ActivePort));
    }

    [RelayCommand]
    private void Unload()
    {
        _appWindowService.IsBorderlessChanged -= OnIsBorderlessChanged;
        _appWindowService.IsPinnedChanged -= OnIsPinnedChanged;
    }

    private void OnIsPinnedChanged(object? sender, bool e)
    {
        OnPropertyChanged(nameof(IsPinned));
    }

    private void OnIsBorderlessChanged(object? sender, bool e)
    {
        OnPropertyChanged(nameof(IsBorderless));
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        _navigationService.Navigate<SettingsPageViewModel>();
    }

    [RelayCommand]
    private async Task ToggleBoardless()
    {
        await _appWindowService.SetBorderlessAsync(!_appWindowService.IsBorderless());
        OnPropertyChanged(nameof(IsBorderless));
    }

    [RelayCommand]
    private void ToggleWindowPin()
    {
        _appWindowService.SetPin(!_appWindowService.IsPinned());
        OnPropertyChanged(nameof(IsPinned));
    }
}

public record HomePageNavigationItem(string Name, MaterialIconKind Icon, Type PageViewModelType);