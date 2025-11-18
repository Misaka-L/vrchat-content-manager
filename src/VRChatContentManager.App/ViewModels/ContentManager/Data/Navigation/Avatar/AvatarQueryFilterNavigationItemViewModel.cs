using Avalonia.Collections;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using VRChatContentManager.App.Services;
using VRChatContentManager.App.ViewModels.ContentManager.Pages.Avatar;
using VRChatContentManager.App.ViewModels.Pages;
using VRChatContentManager.Core.Management.Models.Entity.Avatar;

namespace VRChatContentManager.App.ViewModels.ContentManager.Data.Navigation.Avatar;

public sealed partial class AvatarQueryFilterNavigationItemViewModel(
    AvatarContentQueryFilterEntity avatarContentQueryFilterEntity,
    [FromKeyedServices(ServicesKeys.ContentManagerWindows)]
    NavigationService navigationService,
    ContentManagerAvatarQueryFilterPageViewModelFactory pageViewModelFactory)
    : ViewModelBase, ITreeNavigationItemViewModel
{
    public string Name => avatarContentQueryFilterEntity.Name;
    public AvaloniaList<ITreeNavigationItemViewModel> Children { get; } = [];

    public bool Match(PageViewModelBase pageViewModel)
    {
        if (pageViewModel is not ContentManagerAvatarQueryFilterPageViewModel page)
            return false;

        return page.Id == avatarContentQueryFilterEntity.Id;
    }

    [RelayCommand]
    private void Navigate()
    {
        var page = pageViewModelFactory.Create(avatarContentQueryFilterEntity);
        navigationService.Navigate(page);
    }
}

public sealed class AvatarQueryFilterNavigationItemViewModelFactory(
    [FromKeyedServices(ServicesKeys.ContentManagerWindows)]
    NavigationService navigationService,
    ContentManagerAvatarQueryFilterPageViewModelFactory pageViewModelFactory)
{
    public AvatarQueryFilterNavigationItemViewModel Create(
        AvatarContentQueryFilterEntity avatarContentQueryFilterEntity)
    {
        return new AvatarQueryFilterNavigationItemViewModel(avatarContentQueryFilterEntity, navigationService,
            pageViewModelFactory);
    }
}