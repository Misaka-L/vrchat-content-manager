using Avalonia.Threading;
using VRChatContentPublisher.App.ViewModels;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;
using VRChatContentPublisher.IpcCore.Services;

namespace VRChatContentPublisher.App.Services;

public sealed class AppWindowService(IWritableOptions<AppSettings> appSettings) : IActivateWindowService
{
    private IAppWindow? _mainWindow;

    public event EventHandler<bool>? IsBorderlessChanged;
    public event EventHandler<bool>? IsPinnedChanged;

    public void Register(IAppWindow window)
    {
        _mainWindow = window;
    }

    public void SetPin(bool isPinned)
    {
        _mainWindow?.SetPin(isPinned);

        IsPinnedChanged?.Invoke(this, isPinned);
    }

    public bool IsPinned()
    {
        return _mainWindow?.IsPinned() ?? false;
    }

    public async ValueTask SetBorderlessAsync(bool borderless)
    {
        _mainWindow?.SetBorderless(borderless);
        await appSettings.UpdateAsync(settings => settings.UseBorderlessWindow = borderless);

        IsBorderlessChanged?.Invoke(this, borderless);
    }

    public bool IsBorderless()
    {
        return appSettings.Value.UseBorderlessWindow;
    }

    public async ValueTask ActivateMainWindowAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() => _mainWindow?.Activate());
    }
}