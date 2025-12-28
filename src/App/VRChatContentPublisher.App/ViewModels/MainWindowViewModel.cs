using CommunityToolkit.Mvvm.ComponentModel;
using VRChatContentPublisher.App.Services;
using VRChatContentPublisher.App.ViewModels.Pages;

namespace VRChatContentPublisher.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, INavigationHost, IAppWindow
{
    [ObservableProperty] public partial PageViewModelBase? CurrentPage { get; private set; }

    [ObservableProperty] public partial bool Pinned { get; private set; }

    public event EventHandler? RequestActivate;

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

    public void Activate()
    {
        RequestActivate?.Invoke(this, EventArgs.Empty);
    }
}