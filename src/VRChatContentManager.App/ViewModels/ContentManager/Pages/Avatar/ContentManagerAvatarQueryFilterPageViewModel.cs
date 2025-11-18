using VRChatContentManager.App.ViewModels.Pages;
using VRChatContentManager.Core.Management.Models.Entity.Avatar;

namespace VRChatContentManager.App.ViewModels.ContentManager.Pages.Avatar;

public sealed partial class ContentManagerAvatarQueryFilterPageViewModel(
    AvatarContentQueryFilterEntity avatarContentQueryFilterEntity) : PageViewModelBase
{
    public int Id => avatarContentQueryFilterEntity.Id;
    public string Name => avatarContentQueryFilterEntity.Name;
}

public sealed class ContentManagerAvatarQueryFilterPageViewModelFactory
{
    public ContentManagerAvatarQueryFilterPageViewModel Create(
        AvatarContentQueryFilterEntity avatarContentQueryFilterEntity)
    {
        return new ContentManagerAvatarQueryFilterPageViewModel(avatarContentQueryFilterEntity);
    }
}