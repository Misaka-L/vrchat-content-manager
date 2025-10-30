﻿using VRChatContentManager.App.ViewModels;

namespace VRChatContentManager.App.Services;

public sealed class AppWindowService
{
    private IAppWindow? _mainWindow;

    public void Register(IAppWindow window)
    {
        _mainWindow = window;
    }
    
    public void SetPin(bool isPinned)
    {
        _mainWindow?.SetPin(isPinned);
    }
    
    public bool IsPinned()
    {
        return _mainWindow?.IsPinned() ?? false;
    }
}