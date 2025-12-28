namespace VRChatContentPublisher.ConnectCore.Exceptions;

public sealed class ContentOwnerUserSessionNotFoundException()
    : Exception("The owner of the content does not have an active user session");