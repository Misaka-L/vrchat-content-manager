namespace VRChatContentPublisher.ConnectCore.Exceptions;

public sealed class ProvideFileIdNotFoundException(string fileId)
    : Exception($"Provided file ID '{fileId}' was not found.")
{
    public string FileId { get; } = fileId;
}