using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VRChatContentPublisher.App.Services;
using VRChatContentPublisher.App.ViewModels.Data.PublishTasks;
using VRChatContentPublisher.Core.Services.UserSession;

namespace VRChatContentPublisher.App.ViewModels.Pages.HomeTab;

public sealed partial class HomeTasksPageViewModel(
    UserSessionManagerService userSessionManagerService,
    NavigationService navigationService,
    AddAccountPageViewModelFactory addAccountPageViewModelFactory,
    PublishTaskManagerContainerViewModelFactory containerViewModelFactory) : PageViewModelBase
{
    public ObservableCollection<PublishTaskManagerContainerViewModel> TaskManagers { get; } = [];

    [ObservableProperty]
    public partial PublishTaskManagerContainerViewModel? SelectedTaskManagerContainerViewModel { get; set; }

    private bool _firstLoad = true;

    [RelayCommand]
    private void Load()
    {
        if (_firstLoad)
        {
            foreach (var session in userSessionManagerService.Sessions)
            {
                AddSessionCore(session);
            }
            
            userSessionManagerService.SessionCreated += OnUserSessionCreated;
            userSessionManagerService.SessionRemoved += OnUserSessionRemoved;
        }

        UpdateSelectedTaskManager();

        _firstLoad = false;
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
            UpdateSelectedTaskManager();
        });
    }

    private void UpdateSelectedTaskManager()
    {
        if (SelectedTaskManagerContainerViewModel is not null &&
            TaskManagers.Contains(SelectedTaskManagerContainerViewModel)) return;

        SelectedTaskManagerContainerViewModel = TaskManagers.FirstOrDefault();
    }

    private void AddSessionCore(UserSessionService session)
    {
        var containerViewModel = containerViewModelFactory.Create(session);
        TaskManagers.Add(containerViewModel);
    }
}