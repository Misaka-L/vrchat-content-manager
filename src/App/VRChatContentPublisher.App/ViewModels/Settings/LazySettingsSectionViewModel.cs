using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace VRChatContentPublisher.App.ViewModels.Settings;

/// <summary>
/// Holds a settings section view model and creates it on demand, the first time the section is
/// expanded. The created view model is kept afterwards, so state entered by the user survives
/// collapsing and re-expanding the section, and the section view is only built once.
/// </summary>
public sealed partial class LazySettingsSectionViewModel<TViewModel>(Func<TViewModel> factory) : ViewModelBase
    where TViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLoaded))]
    public partial TViewModel? Content { get; private set; }

    [ObservableProperty] public partial bool IsExpanded { get; set; }

    public bool IsLoaded => Content is not null;

    partial void OnIsExpandedChanged(bool value)
    {
        if (!value)
            return;

        Content ??= factory();
    }
}

/// <summary>
/// Creates <see cref="LazySettingsSectionViewModel{TViewModel}"/> instances that resolve their
/// section view model from the service provider when the section is expanded for the first time,
/// instead of constructing every section when the settings page is opened.
/// </summary>
public sealed class LazySettingsSectionViewModelFactory(IServiceProvider serviceProvider)
{
    public LazySettingsSectionViewModel<TViewModel> Create<TViewModel>() where TViewModel : ViewModelBase
    {
        return new LazySettingsSectionViewModel<TViewModel>(
            () => serviceProvider.GetRequiredService<TViewModel>());
    }
}