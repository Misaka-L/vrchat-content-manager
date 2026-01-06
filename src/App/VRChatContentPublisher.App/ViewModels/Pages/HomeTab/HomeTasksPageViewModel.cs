using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VRChatContentPublisher.App.Services;
using VRChatContentPublisher.App.ViewModels.Data.PublishTasks;
using VRChatContentPublisher.Core.Services.PublishTask;
using VRChatContentPublisher.Core.Services.UserSession;

namespace VRChatContentPublisher.App.ViewModels.Pages.HomeTab;

public sealed partial class HomeTasksPageViewModel(
    PublishTaskManagerViewModelFactory managerViewModelFactory,
    UserSessionManagerService userSessionManagerService,
    NavigationService navigationService,
    AddAccountPageViewModelFactory addAccountPageViewModelFactory,
    ILogger<HomeTasksPageViewModel> logger) : PageViewModelBase
{
    public ObservableCollection<IPublishTaskManagerViewModel> TaskManagers { get; } = [];

    [RelayCommand]
    private async Task Load()
    {
        foreach (var session in userSessionManagerService.Sessions)
        {
            await AddSessionCoreAsync(session);
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
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            if (TaskManagers.Any(s =>
                    s.UserSessionService.UserNameOrEmail == session.UserNameOrEmail ||
                    s.UserSessionService.UserId == session.UserId))
            {
                return;
            }

            await AddSessionCoreAsync(session);
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

    private async ValueTask AddSessionCoreAsync(UserSessionService session)
    {
        try
        {
            var scope = await session.CreateOrGetSessionScopeAsync();
            var managerService = scope.ServiceProvider.GetRequiredService<TaskManagerService>();

            var managerViewModel = managerViewModelFactory.Create(
                session,
                managerService,
                session.CurrentUser?.DisplayName ?? session.UserNameOrEmail
            );

            TaskManagers.Add(managerViewModel);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to get task manager for user session {UserNameOrEmail}",
                session.UserNameOrEmail);
            TaskManagers.Add(new InvalidSessionTaskManagerViewModel(ex, session));
        }
    }
}