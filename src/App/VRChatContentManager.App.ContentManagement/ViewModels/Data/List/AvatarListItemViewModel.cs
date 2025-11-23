using VRChatContentManager.Core.Management.Models.Entity.Avatar;

namespace VRChatContentManager.App.ContentManagement.ViewModels.Data.List;

public sealed class AvatarListItemViewModel(AvatarContentEntity avatarContentEntity)
{
    public string? ThumbnailUrl => avatarContentEntity.ThumbnailImageUrl;

    public string Id => avatarContentEntity.Id;
    public string Name => avatarContentEntity.Name;
}

public sealed class AvatarListItemViewModelFactory
{
    public AvatarListItemViewModel Create(AvatarContentEntity avatarContentEntity)
    {
        return new AvatarListItemViewModel(avatarContentEntity);
    }
}