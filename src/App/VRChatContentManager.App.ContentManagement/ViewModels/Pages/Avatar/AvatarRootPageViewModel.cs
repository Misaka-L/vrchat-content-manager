using CommunityToolkit.Mvvm.Input;
using VRChatContentManager.App.Shared.ViewModels.Pages;
using VRChatContentManager.Core.Management.Services;

namespace VRChatContentManager.App.ContentManagement.ViewModels.Pages.Avatar;

public sealed partial class AvatarRootPageViewModel(
    AvatarContentManagementService avatarContentManagementService)
    : PageViewModelBase
{
    [RelayCommand]
    private async Task Load()
    {
        await avatarContentManagementService.GetAllAvatarsAsync();
    }
}