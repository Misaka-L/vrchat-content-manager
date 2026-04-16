using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VRChatContentPublisher.App.Services;
using VRChatContentPublisher.App.Services.Dialog;
using VRChatContentPublisher.App.ViewModels.Pages;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;

namespace VRChatContentPublisher.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, INavigationHost, IAppWindow
{
    private readonly DialogService _dialogService;
    [ObservableProperty] public partial PageViewModelBase? CurrentPage { get; private set; }

    [ObservableProperty] public partial bool Pinned { get; private set; }
    [ObservableProperty] public partial bool Borderless { get; private set; }

    public event EventHandler? RequestActivate;

    public string DialogHostId { get; }

    public MainWindowViewModel(
        NavigationService navigationService,
        DialogService dialogService,
        AppWindowService appWindowService,
        IWritableOptions<AppSettings> appSettings
    )
    {
        _dialogService = dialogService;

        SetBorderless(appSettings.Value.UseBorderlessWindow);
        DialogHostId = dialogService.DialogHostId;

        navigationService.Register(this);
        appWindowService.Register(this);

        navigationService.Navigate<BootstrapPageViewModel>();
    }

    [RelayCommand]
    private void Load()
    {
        _dialogService.DialogHostReady();
    }

    public void Navigate(PageViewModelBase pageViewModel)
    {
        CurrentPage = pageViewModel;
    }

    public void SetBorderless(bool borderless)
    {
        Borderless = borderless;
    }

    public void SetPin(bool isPinned) => Pinned = isPinned;
    public bool IsPinned() => Pinned;

    public void Activate()
    {
        RequestActivate?.Invoke(this, EventArgs.Empty);
    }
}