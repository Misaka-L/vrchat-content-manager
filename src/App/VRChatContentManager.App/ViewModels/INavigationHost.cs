using VRChatContentManager.App.ViewModels.Pages;

namespace VRChatContentManager.App.ViewModels;

public interface INavigationHost
{
    public void Navigate(PageViewModelBase pageViewModel);
}