namespace VRChatContentManager.App.ViewModels;

public interface IAppWindow
{
    void SetPin(bool isPinned);
    bool IsPinned();
}