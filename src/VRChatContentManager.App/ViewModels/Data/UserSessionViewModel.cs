using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using VRChatContentManager.App.Services;
using VRChatContentManager.App.ViewModels.Pages.Settings;
using VRChatContentManager.Core.Services.UserSession;

namespace VRChatContentManager.App.ViewModels.Data;

public sealed partial class UserSessionViewModel(
    UserSessionService userSessionService,
    UserSessionManagerService userSessionManagerService,
    NavigationService navigationService,
    SettingsFixAccountPageViewModelFactory settingsFixAccountPageViewModelFactory) : ViewModelBase
{
    public string? UserId => userSessionService.UserId;
    public string UserNameOrEmail => userSessionService.UserNameOrEmail;
    public bool IsSessionRequiringReauthentication => userSessionService.State != UserSessionState.LoggedIn;

    public string? ProfilePictureUrl
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(userSessionService.CurrentUser?.ProfilePictureThumbnailUrl))
                return userSessionService.CurrentUser?.ProfilePictureThumbnailUrl;

            return userSessionService.CurrentUser?.AvatarThumbnailImageUrl;
        }
    }

    public string? DisplayName => userSessionService.CurrentUser?.DisplayName;

    [RelayCommand]
    private void Load()
    {
        userSessionService.StateChanged += OnUserSessionStateChanged;
    }

    [RelayCommand]
    private void Unload()
    {
        userSessionService.StateChanged -= OnUserSessionStateChanged;
    }

    [RelayCommand]
    private async Task Remove()
    {
        await userSessionManagerService.RemoveSessionAsync(userSessionService);
    }

    [RelayCommand]
    private void Repair()
    {
        var fixPageViewModel = settingsFixAccountPageViewModelFactory.Create(userSessionService);
        navigationService.Navigate(fixPageViewModel);
    }

    private void OnUserSessionStateChanged(object? sender, UserSessionState e)
    {
        OnPropertyChanged(nameof(IsSessionRequiringReauthentication));
    }
}

public sealed class UserSessionViewModelFactory(
    UserSessionManagerService userSessionManagerService,
    NavigationService navigationService,
    SettingsFixAccountPageViewModelFactory settingsFixAccountPageViewModelFactory)
{
    public UserSessionViewModel Create(UserSessionService userSessionService)
    {
        return new UserSessionViewModel(userSessionService,
            userSessionManagerService,
            navigationService,
            settingsFixAccountPageViewModelFactory);
    }
}