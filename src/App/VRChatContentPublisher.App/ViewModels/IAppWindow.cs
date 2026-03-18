namespace VRChatContentPublisher.App.ViewModels;

public interface IAppWindow
{
    void SetBorderless(bool borderless);
    void SetPin(bool isPinned);
    bool IsPinned();
    void Activate();
}