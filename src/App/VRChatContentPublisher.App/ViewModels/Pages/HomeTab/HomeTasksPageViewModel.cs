using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MessagePipe;
using VRChatContentPublisher.App.Services;
using VRChatContentPublisher.App.ViewModels.Data.PublishTasks;
using VRChatContentPublisher.Core.Events.PublicIp;
using VRChatContentPublisher.Core.Services.PublicIp;
using VRChatContentPublisher.Core.Services.UserSession;

namespace VRChatContentPublisher.App.ViewModels.Pages.HomeTab;

public sealed partial class HomeTasksPageViewModel(
    UserSessionManagerService userSessionManagerService,
    NavigationService navigationService,
    AddAccountPageViewModelFactory addAccountPageViewModelFactory,
    PublishTaskManagerContainerViewModelFactory containerViewModelFactory,
    ISubscriber<PublicIpChangedEvent> publicIpChangedSubscriber,
    PublicIpCheckerService publicIpCheckerService) : PageViewModelBase
{
    public ObservableCollection<PublishTaskManagerContainerViewModel> TaskManagers { get; } = [];

    [ObservableProperty]
    public partial PublishTaskManagerContainerViewModel? SelectedTaskManagerContainerViewModel { get; set; }

    public bool IsPublicIpWarningVisible => !publicIpCheckerService.IsWarningDismissed;
    public string? LastPublicIp => publicIpCheckerService.LastPublicIp;
    public string? LastPreviousPublicIp => publicIpCheckerService.LastPreviousIp;

    private IDisposable? _publicIpChangedSubscription;

    private bool _firstLoad = true;

    [RelayCommand]
    private void Load()
    {
        var preferDefaultSession = _firstLoad;

        var sessionToAdd = userSessionManagerService.Sessions
            .Where(s => TaskManagers.All(manager => manager.UserSessionService != s))
            .ToArray();

        foreach (var session in sessionToAdd)
        {
            AddSessionCore(session);
        }

        var sessionToRemove = TaskManagers
            .Where(manager => !userSessionManagerService.Sessions.Contains(manager.UserSessionService))
            .ToArray();

        foreach (var session in sessionToRemove)
        {
            TaskManagers.Remove(session);
        }

        userSessionManagerService.SessionCreated += OnUserSessionCreated;
        userSessionManagerService.SessionRemoved += OnUserSessionRemoved;
        _publicIpChangedSubscription = publicIpChangedSubscriber.Subscribe(OnPublicIpChanged);

        UpdateSelectedTaskManager(preferDefaultSession);
        _firstLoad = false;
    }

    [RelayCommand]
    private void Unload()
    {
        userSessionManagerService.SessionCreated -= OnUserSessionCreated;
        userSessionManagerService.SessionRemoved -= OnUserSessionRemoved;

        _publicIpChangedSubscription?.Dispose();
        _publicIpChangedSubscription = null;
    }

    [RelayCommand]
    private async Task DismissPublicIpWarning()
    {
        await publicIpCheckerService.DismissWarningAsync();

        OnPropertyChanged(nameof(LastPublicIp));
        OnPropertyChanged(nameof(LastPreviousPublicIp));
        OnPropertyChanged(nameof(IsPublicIpWarningVisible));
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

    private void UpdateSelectedTaskManager(bool preferDefaultSession = false)
    {
        if (SelectedTaskManagerContainerViewModel is not null &&
            TaskManagers.Contains(SelectedTaskManagerContainerViewModel)) return;

        if (preferDefaultSession && userSessionManagerService.GetDefaultSession() is { } defaultSession)
        {
            SelectedTaskManagerContainerViewModel = TaskManagers.FirstOrDefault(manager =>
                manager.UserSessionService == defaultSession);

            if (SelectedTaskManagerContainerViewModel is not null)
                return;
        }

        SelectedTaskManagerContainerViewModel = TaskManagers.FirstOrDefault();
    }

    private void AddSessionCore(UserSessionService session)
    {
        var containerViewModel = containerViewModelFactory.Create(session);
        TaskManagers.Add(containerViewModel);
    }

    private void OnPublicIpChanged(PublicIpChangedEvent args)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            OnPropertyChanged(nameof(LastPublicIp));
            OnPropertyChanged(nameof(LastPreviousPublicIp));
            OnPropertyChanged(nameof(IsPublicIpWarningVisible));
        });
    }
}