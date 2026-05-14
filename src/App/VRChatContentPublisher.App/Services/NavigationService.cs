using Microsoft.Extensions.DependencyInjection;
using VRChatContentPublisher.App.ViewModels;
using VRChatContentPublisher.App.ViewModels.Pages;

namespace VRChatContentPublisher.App.Services;

public class NavigationService(IServiceProvider serviceProvider)
{
    private INavigationHost? _navigationHost;

    public void Register(INavigationHost navigationHost)
    {
        _navigationHost = navigationHost;
    }

    public void Navigate(PageViewModelBase pageViewModel)
    {
        _navigationHost?.Navigate(pageViewModel);
    }

    public void Navigate<T>() where T : PageViewModelBase
    {
        var pageViewModel = serviceProvider.GetRequiredService<T>();
        Navigate(pageViewModel);
    }

    public void Navigate<T>(Action<T> configure) where T : PageViewModelBase
    {
        var pageViewModel = serviceProvider.GetRequiredService<T>();
        configure(pageViewModel);
        Navigate(pageViewModel);
    }

    public void Navigate<TFactory, TPageViewModel>(
        Func<TFactory, TPageViewModel> factory
    ) where TPageViewModel : PageViewModelBase where TFactory : notnull
    {
        var pageViewModelFactory = serviceProvider.GetRequiredService<TFactory>();
        Navigate(factory(pageViewModelFactory));
    }
}