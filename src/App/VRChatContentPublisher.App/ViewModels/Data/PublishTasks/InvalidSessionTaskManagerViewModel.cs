using CommunityToolkit.Mvvm.Input;
using VRChatContentPublisher.App.Services;
using VRChatContentPublisher.App.ViewModels.Pages.Settings;
using VRChatContentPublisher.Core.Services.UserSession;

namespace VRChatContentPublisher.App.ViewModels.Data.PublishTasks;

public sealed partial class InvalidSessionTaskManagerViewModel(
    Exception exception,
    UserSessionService userSessionService,
    NavigationService navigationService,
    SettingsFixAccountPageViewModelFactory fixAccountPageViewModelFactory
)
    : ViewModelBase, IPublishTaskManagerViewModel
{
    public Exception Exception => exception;
    public string ExceptionMessage => exception.Message;
    public string ExceptionString => exception.ToString();

    public string DisplayName => userSessionService.CurrentUser?.DisplayName ?? userSessionService.UserNameOrEmail;

    public UserSessionService UserSessionService => userSessionService;

    [RelayCommand]
    private async Task Repair()
    {
        if (await userSessionService.TryRepairAsync())
            return;

        var page = fixAccountPageViewModelFactory.Create(userSessionService);
        navigationService.Navigate(page);
    }
}

public sealed class InvalidSessionTaskManagerViewModelFactory(
    NavigationService navigationService,
    SettingsFixAccountPageViewModelFactory fixAccountPageViewModelFactory
)
{
    public InvalidSessionTaskManagerViewModel Create(Exception exception, UserSessionService userSessionService)
    {
        return new InvalidSessionTaskManagerViewModel(
            exception,
            userSessionService,
            navigationService,
            fixAccountPageViewModelFactory
        );
    }
}