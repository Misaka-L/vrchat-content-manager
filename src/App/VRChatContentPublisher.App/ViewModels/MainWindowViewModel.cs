using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VRChatContentPublisher.App.Localization;
using VRChatContentPublisher.App.Services;
using VRChatContentPublisher.App.Services.Dialog;
using VRChatContentPublisher.App.Services.Notification;
using VRChatContentPublisher.App.ViewModels.Pages;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;

namespace VRChatContentPublisher.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, INavigationHost, IAppWindow
{
    private readonly DialogService _dialogService;
    private readonly IWritableOptions<AppSettings> _appSettings;
    private readonly DesktopNotificationService _desktopNotificationService;
    [ObservableProperty] public partial PageViewModelBase? CurrentPage { get; private set; }

    [ObservableProperty] public partial bool Pinned { get; private set; }
    [ObservableProperty] public partial bool Borderless { get; private set; }

    public event EventHandler? RequestActivate;

    public string DialogHostId { get; }

    public MainWindowViewModel(
        NavigationService navigationService,
        DialogService dialogService,
        AppWindowService appWindowService,
        IWritableOptions<AppSettings> appSettings,
        DesktopNotificationService desktopNotificationService
    )
    {
        _dialogService = dialogService;
        _appSettings = appSettings;
        _desktopNotificationService = desktopNotificationService;

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

    public void NotifyWindowHide()
    {
        if (_appSettings.Value.DismissFirstTimeHideWindowTip)
            return;

        _appSettings.Update(settings => settings.DismissFirstTimeHideWindowTip = true);
        _ = _desktopNotificationService.SendNotificationAsync(
            LangKeys.Notifications_First_Time_Hide_Window_Tip_Title,
            LangKeys.Notifications_First_Time_Hide_Window_Tip_Message
        ).AsTask();
    }
}