using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VRChatContentManager.App.Services;
using VRChatContentManager.App.ViewModels.Data;
using VRChatContentManager.Core.Services.UserSession;

namespace VRChatContentManager.App.ViewModels.Pages;

public sealed partial class SettingsPageViewModel(
    NavigationService navigationService,
    UserSessionManagerService userSessionManagerService,
    UserSessionViewModelFactory userSessionViewModelFactory) : PageViewModelBase
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
    private void NavigateToHome()
    {
        navigationService.Navigate<HomePageViewModel>();
    }
}