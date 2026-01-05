using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VRChatContentPublisher.App.Services;
using VRChatContentPublisher.App.ViewModels.Pages.Settings;
using VRChatContentPublisher.Core.Services.PublishTask;
using VRChatContentPublisher.Core.Services.UserSession;

namespace VRChatContentPublisher.App.ViewModels.Data;

public sealed partial class UserSessionViewModel(
    UserSessionService userSessionService,
    ILogger<UserSessionViewModel> logger,
    UserSessionManagerService userSessionManagerService,
    NavigationService navigationService,
    SettingsFixAccountPageViewModelFactory settingsFixAccountPageViewModelFactory) : ViewModelBase
{
    public string? UserId => userSessionService.UserId;
    public string UserNameOrEmail => userSessionService.UserNameOrEmail;
    public bool IsSessionRequiringReauthentication => userSessionService.State != UserSessionState.LoggedIn;

    [ObservableProperty] public partial bool CanRemove { get; private set; }

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
    private async Task Load()
    {
        userSessionService.StateChanged += OnUserSessionStateChanged;

        await LoadCore();
    }

    private async ValueTask LoadCore()
    {
        if (userSessionService.State != UserSessionState.LoggedIn)
        {
            CanRemove = true;
            return;
        }

        try
        {
            var scope = await userSessionService.CreateOrGetSessionScopeAsync();
            var taskManager = scope.ServiceProvider.GetRequiredService<TaskManagerService>();

            CanRemove = taskManager.Tasks.Count == 0;
        }
        catch
        {
            // ignored
        }
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
    private async Task Repair()
    {
        try
        {
            await userSessionService.GetCurrentUserAsync();
            return;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to check is account valid. Navigating to fix account page.");
        }

        var fixPageViewModel = settingsFixAccountPageViewModelFactory.Create(userSessionService);
        navigationService.Navigate(fixPageViewModel);
    }

    private async void OnUserSessionStateChanged(object? sender, UserSessionState e)
    {
        await LoadCore();

        OnPropertyChanged(nameof(IsSessionRequiringReauthentication));
        OnPropertyChanged(nameof(UserId));
        OnPropertyChanged(nameof(UserNameOrEmail));
        OnPropertyChanged(nameof(ProfilePictureUrl));
        OnPropertyChanged(nameof(DisplayName));
    }
}

public sealed class UserSessionViewModelFactory(
    ILogger<UserSessionViewModel> logger,
    UserSessionManagerService userSessionManagerService,
    NavigationService navigationService,
    SettingsFixAccountPageViewModelFactory settingsFixAccountPageViewModelFactory)
{
    public UserSessionViewModel Create(UserSessionService userSessionService)
    {
        return new UserSessionViewModel(
            userSessionService,
            logger,
            userSessionManagerService,
            navigationService,
            settingsFixAccountPageViewModelFactory);
    }
}