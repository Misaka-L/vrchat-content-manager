namespace VRChatContentPublisher.App.ViewModels;

public interface IAppWindow
{
    void SetPin(bool isPinned);
    bool IsPinned();
    void Activate();
}