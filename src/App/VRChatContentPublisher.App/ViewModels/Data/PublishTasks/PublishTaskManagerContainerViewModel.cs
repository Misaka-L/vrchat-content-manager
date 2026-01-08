using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VRChatContentPublisher.Core.Models.VRChatApi.Rest.Auth;
using VRChatContentPublisher.Core.Services.PublishTask;
using VRChatContentPublisher.Core.Services.UserSession;

namespace VRChatContentPublisher.App.ViewModels.Data.PublishTasks;

public sealed partial class PublishTaskManagerContainerViewModel(
    UserSessionService userSessionService,
    PublishTaskManagerViewModelFactory managerViewModelFactory,
    InvalidSessionTaskManagerViewModelFactory invalidSessionTaskManagerViewModelFactory,
    ILogger<PublishTaskManagerContainerViewModel> logger
) : ViewModelBase
{
    public string DisplayName =>
        userSessionService.CurrentUser?.DisplayName ?? userSessionService.UserNameOrEmail;

    public string? AvatarUri
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(userSessionService.CurrentUser?.ProfilePictureThumbnailUrl))
                return userSessionService.CurrentUser?.ProfilePictureThumbnailUrl;

            return userSessionService.CurrentUser?.AvatarThumbnailImageUrl;
        }
    }

    public UserSessionService UserSessionService => userSessionService;

    [ObservableProperty] public partial IPublishTaskManagerViewModel PublishTaskManager { get; private set; }

    [RelayCommand]
    private async Task Load()
    {
        await LoadCoreAsync();

        userSessionService.StateChanged += OnSessionStateChanged;
        userSessionService.CurrentUserUpdated += OnCurrentUserUpdated;
    }

    [RelayCommand]
    private void Unload()
    {
        userSessionService.StateChanged -= OnSessionStateChanged;
        userSessionService.CurrentUserUpdated -= OnCurrentUserUpdated;
    }

    private void OnCurrentUserUpdated(object? sender, CurrentUser? e)
    {
        Dispatcher.UIThread.Invoke(NotifyCurrentUserUpdate);
    }

    private void OnSessionStateChanged(object? sender, UserSessionState e)
    {
        Dispatcher.UIThread.InvokeAsync(LoadCoreAsync);
    }

    private async ValueTask LoadCoreAsync()
    {
        try
        {
            var scope = await userSessionService.CreateOrGetSessionScopeAsync();
            var managerService = scope.ServiceProvider.GetRequiredService<TaskManagerService>();

            var managerViewModel = managerViewModelFactory.Create(
                userSessionService,
                managerService,
                userSessionService.CurrentUser?.DisplayName ?? userSessionService.UserNameOrEmail
            );

            PublishTaskManager = managerViewModel;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to get task manager for user session {UserNameOrEmail}",
                userSessionService.UserNameOrEmail
            );

            PublishTaskManager = invalidSessionTaskManagerViewModelFactory.Create(ex, userSessionService);
        }
    }

    private void NotifyCurrentUserUpdate()
    {
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(AvatarUri));
    }
}

public sealed class PublishTaskManagerContainerViewModelFactory(
    PublishTaskManagerViewModelFactory managerViewModelFactory,
    InvalidSessionTaskManagerViewModelFactory invalidSessionTaskManagerViewModelFactory,
    ILogger<PublishTaskManagerContainerViewModel> logger
)
{
    public PublishTaskManagerContainerViewModel Create(UserSessionService userSessionService)
    {
        return new PublishTaskManagerContainerViewModel(
            userSessionService,
            managerViewModelFactory,
            invalidSessionTaskManagerViewModelFactory,
            logger
        );
    }
}