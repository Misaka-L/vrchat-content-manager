namespace VRChatContentPublisher.IpcCore.Exceptions;

public class MutexOwnedByAnotherInstanceException() : Exception("The application mutex is owned by another instance.");