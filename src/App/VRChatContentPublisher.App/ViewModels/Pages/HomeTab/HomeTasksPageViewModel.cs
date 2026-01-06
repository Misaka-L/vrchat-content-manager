using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using VRChatContentPublisher.App.Services;
using VRChatContentPublisher.App.ViewModels.Data.PublishTasks;
using VRChatContentPublisher.Core.Services.UserSession;

namespace VRChatContentPublisher.App.ViewModels.Pages.HomeTab;

public sealed partial class HomeTasksPageViewModel(
    UserSessionManagerService userSessionManagerService,
    NavigationService navigationService,
    AddAccountPageViewModelFactory addAccountPageViewModelFactory,
    PublishTaskManagerContainerViewModelFactory containerViewModelFactory,
    ILogger<HomeTasksPageViewModel> logger) : PageViewModelBase
{
    public ObservableCollection<PublishTaskManagerContainerViewModel> TaskManagers { get; } = [];

    [RelayCommand]
    private void Load()
    {
        foreach (var session in userSessionManagerService.Sessions)
        {
            AddSessionCore(session);
        }

        userSessionManagerService.SessionCreated += OnUserSessionCreated;
        userSessionManagerService.SessionRemoved += OnUserSessionRemoved;
    }

    [RelayCommand]
    private void Unload()
    {
        userSessionManagerService.SessionCreated -= OnUserSessionCreated;
        userSessionManagerService.SessionRemoved -= OnUserSessionRemoved;

        TaskManagers.Clear();
    }

    [RelayCommand]
    private void Login()
    {
        var addAccountPage = addAccountPageViewModelFactory.Create(
            navigationService.Navigate<HomePageViewModel>,
            navigationService.Navigate<HomePageViewModel>
        );

        navigationService.Navigate(addAccountPage);
    }

    private void OnUserSessionCreated(object? sender, UserSessionService session)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            if (TaskManagers.Any(s =>
                    s.UserSessionService.UserNameOrEmail == session.UserNameOrEmail ||
                    s.UserSessionService.UserId == session.UserId))
            {
                return;
            }

            AddSessionCore(session);
        });
    }

    private void OnUserSessionRemoved(object? sender, UserSessionService e)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            if (TaskManagers.FirstOrDefault(s =>
                    s.UserSessionService.UserNameOrEmail == e.UserNameOrEmail ||
                    s.UserSessionService.UserId == e.UserId)
                is not { } matchViewModel)
            {
                return;
            }

            TaskManagers.Remove(matchViewModel);
        });
    }

    private void AddSessionCore(UserSessionService session)
    {
        var containerViewModel = containerViewModelFactory.Create(session);
        TaskManagers.Add(containerViewModel);
    }
}