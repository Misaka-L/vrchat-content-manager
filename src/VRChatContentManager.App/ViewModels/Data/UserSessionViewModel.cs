using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using VRChatContentManager.Core.Services.UserSession;

namespace VRChatContentManager.App.ViewModels.Data;

public sealed partial class UserSessionViewModel(
    UserSessionService userSessionService,
    UserSessionManagerService userSessionManagerService) : ViewModelBase
{
    public string? UserId => userSessionService.UserId;
    public string UserNameOrEmail => userSessionService.UserNameOrEmail;

    public string? ProfilePictureUrl
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(userSessionService.CurrentUser?.ProfilePictureThumbnailUrl))
                return userSessionService.CurrentUser?.ProfilePictureThumbnailUrl;

            return userSessionService.CurrentUser?.AvatarThumbnailImageUrl;
        }
    }

    public string? DisplayName => userSessionService.CurrentUser?.DisplayName;

    [RelayCommand]
    private async Task Remove()
    {
        await userSessionManagerService.RemoveSessionAsync(userSessionService);
    }
}

public sealed class UserSessionViewModelFactory(UserSessionManagerService userSessionManagerService)
{
    public UserSessionViewModel Create(UserSessionService userSessionService)
    {
        return new UserSessionViewModel(userSessionService, userSessionManagerService);
    }
}