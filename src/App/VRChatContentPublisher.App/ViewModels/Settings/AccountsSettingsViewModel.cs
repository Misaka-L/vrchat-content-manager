using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VRChatContentPublisher.App.Services;
using VRChatContentPublisher.App.ViewModels.Data;
using VRChatContentPublisher.App.ViewModels.Pages;
using VRChatContentPublisher.Core.Services.UserSession;

namespace VRChatContentPublisher.App.ViewModels.Settings;

public sealed partial class AccountsSettingsViewModel(
    UserSessionManagerService userSessionManagerService,
    UserSessionViewModelFactory userSessionViewModelFactory,
    NavigationService navigationService,
    AddAccountPageViewModelFactory addAccountPageViewModelFactory
) : ViewModelBase
{
    [ObservableProperty] public partial AvaloniaList<UserSessionViewModel> UserSessions { get; private set; } = [];

    [RelayCommand]
    private void Load()
    {
        UpdateSessions();

        userSessionManagerService.SessionCreated += OnSessionCreated;
        userSessionManagerService.SessionRemoved += OnSessionRemoved;
    }

    [RelayCommand]
    private void Unload()
    {
        userSessionManagerService.SessionCreated -= OnSessionCreated;
        userSessionManagerService.SessionRemoved -= OnSessionRemoved;
    }

    private void OnSessionRemoved(object? sender, UserSessionService e) => UpdateSessions();
    private void OnSessionCreated(object? sender, UserSessionService e) => UpdateSessions();

    private void UpdateSessions()
    {
        UserSessions.Clear();
        var viewModels = userSessionManagerService.Sessions
            .Select(userSessionViewModelFactory.Create)
            .ToArray();

        UserSessions.AddRange(viewModels);
    }

    [RelayCommand]
    private void AddNewAccount()
    {
        var addAccountPageViewModel = addAccountPageViewModelFactory.Create(
            navigationService.Navigate<SettingsPageViewModel>,
            navigationService.Navigate<SettingsPageViewModel>);

        navigationService.Navigate(addAccountPageViewModel);
    }
}