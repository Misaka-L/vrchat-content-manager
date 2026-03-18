using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MessagePipe;
using VRChatContentPublisher.App.Services;
using VRChatContentPublisher.App.ViewModels.Data.PublishTasks;
using VRChatContentPublisher.Core.Events.PublicIp;
using VRChatContentPublisher.Core.Services.UserSession;

namespace VRChatContentPublisher.App.ViewModels.Pages.HomeTab;

public sealed partial class HomeTasksPageViewModel(
    UserSessionManagerService userSessionManagerService,
    NavigationService navigationService,
    AddAccountPageViewModelFactory addAccountPageViewModelFactory,
    PublishTaskManagerContainerViewModelFactory containerViewModelFactory,
    ISubscriber<PublicIpChangedEvent> publicIpChangedSubscriber) : PageViewModelBase
{
    public ObservableCollection<PublishTaskManagerContainerViewModel> TaskManagers { get; } = [];

    [ObservableProperty]
    public partial PublishTaskManagerContainerViewModel? SelectedTaskManagerContainerViewModel { get; set; }

    [ObservableProperty] public partial bool IsPublicIpWarningVisible { get; private set; }

    [ObservableProperty] public partial string PublicIpWarningText { get; private set; } = string.Empty;

    private Guid? _currentPublicIpWarningInstanceId;
    private Guid? _dismissedPublicIpWarningInstanceId;
    private IDisposable? _publicIpChangedSubscription;

    private bool _firstLoad = true;

    [RelayCommand]
    private void Load()
    {
        var preferDefaultSession = _firstLoad;

        if (_firstLoad)
        {
            foreach (var session in userSessionManagerService.Sessions)
            {
                AddSessionCore(session);
            }
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
    private void DismissPublicIpWarning()
    {
        _dismissedPublicIpWarningInstanceId = _currentPublicIpWarningInstanceId;
        IsPublicIpWarningVisible = false;
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
            if (_dismissedPublicIpWarningInstanceId == args.WarningInstanceId)
                return;

            _currentPublicIpWarningInstanceId = args.WarningInstanceId;
            PublicIpWarningText = $"Public IP changed from {args.OldIpPlaintext} to {args.NewIpPlaintext}.";
            IsPublicIpWarningVisible = true;
        });
    }
}