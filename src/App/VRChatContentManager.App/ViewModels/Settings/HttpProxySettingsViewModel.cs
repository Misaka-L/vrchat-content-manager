using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VRChatContentManager.Core.Settings;
using VRChatContentManager.Core.Settings.Models;

namespace VRChatContentManager.App.ViewModels.Settings;

public sealed partial class HttpProxySettingsViewModel : ViewModelBase
{
    private readonly IWritableOptions<AppSettings> _appSettings;

    [ObservableProperty] public partial string ProxyUri { get; set; }

    [NotifyPropertyChangedFor(nameof(IsCustomProxySelected))]
    [ObservableProperty]
    public partial HttpProxyModeItemViewModel SelectedHttpProxyMode { get; set; }

    public bool IsCustomProxySelected => SelectedHttpProxyMode.Mode == AppHttpProxySettings.CustomProxy;

    public HttpProxyModeItemViewModel[] HttpProxyModes { get; } =
    [
        new("System Default", AppHttpProxySettings.SystemProxy),
        new("No Proxy", AppHttpProxySettings.NoProxy),
        new("Custom Proxy", AppHttpProxySettings.CustomProxy)
    ];

    public HttpProxySettingsViewModel(IWritableOptions<AppSettings> appSettings)
    {
        _appSettings = appSettings;
        UpdateSettingsFromOptions();
    }

    [RelayCommand]
    private void Load()
    {
        UpdateSettingsFromOptions();
    }

    private void UpdateSettingsFromOptions()
    {
        ProxyUri = _appSettings.Value.HttpProxyUri?.ToString() ?? "";
        SelectedHttpProxyMode = HttpProxyModes.First(x => x.Mode == _appSettings.Value.HttpProxySettings);
    }

    [RelayCommand]
    private async Task ApplyHttpProxySettings()
    {
        if (!Uri.TryCreate(ProxyUri, UriKind.Absolute, out var uri) && IsCustomProxySelected)
            return;

        await _appSettings.UpdateAsync(settings =>
        {
            settings.HttpProxyUri = uri;
            settings.HttpProxySettings = SelectedHttpProxyMode.Mode;
        });

        UpdateSettingsFromOptions();
    }
}

public record HttpProxyModeItemViewModel(string Name, AppHttpProxySettings Mode);