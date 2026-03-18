using CommunityToolkit.Mvvm.ComponentModel;
using VRChatContentPublisher.App.Services;
using VRChatContentPublisher.App.ViewModels.Pages;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;

namespace VRChatContentPublisher.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, INavigationHost, IAppWindow
{
    [ObservableProperty] public partial PageViewModelBase? CurrentPage { get; private set; }

    [ObservableProperty] public partial bool Pinned { get; private set; }
    [ObservableProperty] public partial bool Borderless { get; private set; }

    public event EventHandler? RequestActivate;

    public string DialogHostId { get; } = "MainWindow-" + Guid.NewGuid().ToString("D");

    public MainWindowViewModel(
        NavigationService navigationService,
        DialogService dialogService,
        AppWindowService appWindowService,
        IWritableOptions<AppSettings> appSettings
    )
    {
        SetBorderless(appSettings.Value.UseBorderlessWindow);
        dialogService.SetDialogHostId(DialogHostId);

        navigationService.Register(this);
        appWindowService.Register(this);

        navigationService.Navigate<BootstrapPageViewModel>();
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