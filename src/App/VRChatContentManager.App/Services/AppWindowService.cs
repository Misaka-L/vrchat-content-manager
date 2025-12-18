using Avalonia.Threading;
using VRChatContentManager.App.ViewModels;
using VRChatContentManager.IpcCore.Services;

namespace VRChatContentManager.App.Services;

public sealed class AppWindowService : IActivateWindowService
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

    public async ValueTask ActivateMainWindowAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() => _mainWindow?.Activate());
    }
}