using VRChatContentManager.App.Shared.ViewModels.Pages;
using VRChatContentManager.Core.Management.Models.Entity.Avatar;

namespace VRChatContentManager.App.ContentManagement.ViewModels.Pages.Avatar;

public sealed partial class AvatarQueryFilterPageViewModel(
    AvatarContentQueryFilterEntity avatarContentQueryFilterEntity) : PageViewModelBase
{
    public int Id => avatarContentQueryFilterEntity.Id;
    public string Name => avatarContentQueryFilterEntity.Name;
}

public sealed class ContentManagerAvatarQueryFilterPageViewModelFactory
{
    public AvatarQueryFilterPageViewModel Create(
        AvatarContentQueryFilterEntity avatarContentQueryFilterEntity)
    {
        return new AvatarQueryFilterPageViewModel(avatarContentQueryFilterEntity);
    }
}