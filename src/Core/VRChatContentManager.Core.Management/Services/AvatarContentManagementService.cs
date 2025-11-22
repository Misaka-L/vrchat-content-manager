using VRChatContentManager.Core.Management.Models.Entity.Avatar;

namespace VRChatContentManager.Core.Management.Services;

public sealed class AvatarContentManagementService(IFreeSql freeSql)
{
    public async ValueTask<List<AvatarContentEntity>> GetAllAvatarsAsync()
    {
        return await freeSql
            .Select<AvatarContentEntity>()
            .IncludeMany(avatar => avatar.LocalTags)
            .ToListAsync();
    }

    public async ValueTask<List<AvatarContentEntity>> GetAvatarsByIdsAsync(string[] ids)
    {
        return await freeSql
            .Select<AvatarContentEntity>()
            .Where(e => ids.Contains(e.Id))
            .Include(avatar => avatar.LocalTags)
            .IncludeMany(avatar => avatar.SupportedPlatform)
            .ToListAsync();
    }

    public async ValueTask<List<AvatarContentQueryFilterEntity>> GetAllQueryFiltersAsync()
    {
        return await freeSql
            .Select<AvatarContentQueryFilterEntity>()
            .ToListAsync();
    }

    public async ValueTask<List<AvatarContentTagEntity>> GetAllTagsAsync()
    {
        return await freeSql
            .Select<AvatarContentTagEntity>()
            .ToListAsync();
    }

    public async ValueTask<List<AvatarContentSupportedPlatformEntity>> GetAllSupportedPlatformsAsync()
    {
        return await freeSql
            .Select<AvatarContentSupportedPlatformEntity>()
            .ToListAsync();
    }
}