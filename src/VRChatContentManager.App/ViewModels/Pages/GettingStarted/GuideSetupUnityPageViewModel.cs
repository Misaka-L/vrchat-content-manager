using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VRChatContentManager.Core.Services.UserSession;

namespace VRChatContentManager.App.ViewModels.Pages.GettingStarted;

public partial class GuideSetupUnityPageViewModel(UserSessionManagerService userSessionManagerService)
    : PageViewModelBase
{
    [ObservableProperty] public partial string UserId { get; private set; } = "";
    [ObservableProperty] public partial string UserName { get; private set; } = "";
    [ObservableProperty] public partial string DisplayName { get; private set; } = "";
    
    [RelayCommand]
    private async Task Load()
    {
        var session = userSessionManagerService.Sessions[0];
        
        var user = await session.GetCurrentUserAsync();
        
        UserId = user.Id;
        UserName = user.UserName;
        DisplayName = user.DisplayName;
    }
    
    [RelayCommand]
    private async Task Logoff()
    {
        foreach (var session in userSessionManagerService.Sessions)
        {
            try
            {
                await session.GetApiClient().LogoutAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.WriteLine("Failed to logout");
            }
        }
    }
}