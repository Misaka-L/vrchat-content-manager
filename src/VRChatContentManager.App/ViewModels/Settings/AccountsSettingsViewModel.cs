using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VRChatContentManager.App.Services;
using VRChatContentManager.App.ViewModels.Data;
using VRChatContentManager.App.ViewModels.Pages.Settings;
using VRChatContentManager.Core.Services.UserSession;

namespace VRChatContentManager.App.ViewModels.Settings;

public sealed partial class AccountsSettingsViewModel(
    UserSessionManagerService userSessionManagerService,
    UserSessionViewModelFactory userSessionViewModelFactory,
    NavigationService navigationService
    ) : ViewModelBase
{
    [ObservableProperty]
    public partial ObservableCollection<UserSessionViewModel> UserSessions { get; private set; } = [];

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
    private void AddNewAccount()
    {
        navigationService.Navigate<SettingsAddAccountPageViewModel>();
    }
}