﻿namespace VRChatContentManager.ConnectCore.Services.PublishTask;

public interface IAvatarPublishTaskService
{
    Task CreatePublishTaskAsync(string avatarId, string avatarBundleFileId, string name, string platform, string unityVersion);
}