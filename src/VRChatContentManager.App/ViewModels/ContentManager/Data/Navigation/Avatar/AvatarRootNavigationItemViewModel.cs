using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using VRChatContentManager.App.Services;
using VRChatContentManager.App.ViewModels.ContentManager.Pages.Avatar;
using VRChatContentManager.App.ViewModels.Pages;
using VRChatContentManager.Core.Management.Services;

namespace VRChatContentManager.App.ViewModels.ContentManager.Data.Navigation.Avatar;

public sealed partial class AvatarRootNavigationItemViewModel(
    [FromKeyedServices(ServicesKeys.ContentManagerWindows)]
    NavigationService navigationService,
    AvatarContentManagementService avatarContentManagementService,
    AvatarQueryFilterNavigationItemViewModelFactory queryFilterNavigationItemViewModelFactory)
    : ViewModelBase, ITreeNavigationItemViewModel
{
    public string Name => "Avatars";
    public AvaloniaList<ITreeNavigationItemViewModel> Children { get; } = [];

    public bool Match(PageViewModelBase pageViewModel) => pageViewModel is ContentManagerAvatarRootPageViewModel;

    [RelayCommand]
    private void Navigate() => navigationService.Navigate<ContentManagerAvatarRootPageViewModel>();

    public async Task LoadChildrenAsync()
    {
        Children.Clear();

        var filters = await avatarContentManagementService.GetAllQueryFiltersAsync();
        var viewModels = filters.Select(queryFilterNavigationItemViewModelFactory.Create).ToArray();

        Children.AddRange(viewModels);
    }
}