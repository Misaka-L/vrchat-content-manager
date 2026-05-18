using CommunityToolkit.Mvvm.Input;
using VRChatContentPublisher.App.Services;
using VRChatContentPublisher.App.ViewModels.Pages;
using VRChatContentPublisher.Core.UserSession;

namespace VRChatContentPublisher.App.ViewModels.Data.PublishTasks;

public sealed partial class InvalidSessionTaskManagerViewModel(
    Exception exception,
    UserSessionService userSessionService,
    NavigationService navigationService,
    LoginPageViewModelFactory loginPageViewModelFactory
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

        var page = loginPageViewModelFactory.Create(
            navigationService.Navigate<HomePageViewModel>,
            navigationService.Navigate<HomePageViewModel>,
            userSessionService.UserNameOrEmail
        );

        navigationService.Navigate(page);
    }
}

public sealed class InvalidSessionTaskManagerViewModelFactory(
    NavigationService navigationService,
    LoginPageViewModelFactory loginPageViewModelFactory
)
{
    public InvalidSessionTaskManagerViewModel Create(Exception exception, UserSessionService userSessionService)
    {
        return new InvalidSessionTaskManagerViewModel(
            exception,
            userSessionService,
            navigationService,
            loginPageViewModelFactory
        );
    }
}