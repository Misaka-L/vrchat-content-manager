﻿using System;
using Microsoft.Extensions.DependencyInjection;
using VRChatContentManager.App.ViewModels;
using VRChatContentManager.App.ViewModels.Pages;

namespace VRChatContentManager.App.Services;

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
}