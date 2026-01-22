namespace VRChatContentPublisher.ConnectCore.Exceptions;

public sealed class ContentOwnerSessionOrAvatarNotFoundException() : Exception(
    "The owner of the content does not have an active user session or the avatar was not exist.");