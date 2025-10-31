using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VRChatContentManager.App.Services;
using VRChatContentManager.App.ViewModels.Data;
using VRChatContentManager.App.ViewModels.Pages.Settings;
using VRChatContentManager.Core.Services.UserSession;
using VRChatContentManager.Core.Settings;
using VRChatContentManager.Core.Settings.Models;

namespace VRChatContentManager.App.ViewModels.Pages;

public sealed partial class SettingsPageViewModel(
    NavigationService navigationService,
    UserSessionManagerService userSessionManagerService,
    UserSessionViewModelFactory userSessionViewModelFactory,
    IWritableOptions<AppSettings> appSettings) : PageViewModelBase
{
    [ObservableProperty]
    public partial ObservableCollection<UserSessionViewModel> UserSessions { get; private set; } = [];

    [ObservableProperty]
    public partial string ConnectInstanceName { get; set; } = appSettings.Value.ConnectInstanceName;

    [RelayCommand]
    private Task Load()
    {
        UserSessions.Clear();
        foreach (var session in userSessionManagerService.Sessions)
        {
            var vm = userSessionViewModelFactory.Create(session);
            UserSessions.Add(vm);
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private void NavigateToHome()
    {
        navigationService.Navigate<HomePageViewModel>();
    }

    [RelayCommand]
    private void AddNewAccount()
    {
        navigationService.Navigate<SettingsAddAccountPageViewModel>();
    }

    [RelayCommand]
    private async Task ApplyConnectSettings()
    {
        await appSettings.UpdateAsync(settings => { settings.ConnectInstanceName = ConnectInstanceName; });
    }
}