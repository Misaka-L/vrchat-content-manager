using VRChatContentPublisher.App.ViewModels.Pages;

namespace VRChatContentPublisher.App.ViewModels;

public interface INavigationHost
{
    public void Navigate(PageViewModelBase pageViewModel);
}