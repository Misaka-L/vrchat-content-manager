using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using VRChatContentPublisher.App.Services;
using VRChatContentPublisher.Core.Models;
using VRChatContentPublisher.Core.Services.PublishTask;
using VRChatContentPublisher.Core.Services.UserSession;

namespace VRChatContentPublisher.App.ViewModels.Dialogs;

public sealed partial class ExitAppDialogViewModel(AppLifetimeService lifetimeService) : DialogViewModelBase
{
    [RelayCommand]
    private void ExitApp()
    {
        lifetimeService.Shutdown();
    }
}