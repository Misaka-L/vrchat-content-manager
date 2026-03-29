using Antelcat.I18N.Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VRChatContentPublisher.App.Localization;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;

namespace VRChatContentPublisher.App.ViewModels.Settings;

public sealed partial class HttpProxySettingsViewModel(IWritableOptions<AppSettings> appSettings) : ViewModelBase
{
    public static string ProxyUriValidationFailedMessage =>
        I18NExtension.Translate(LangKeys.Pages_Settings_Http_Proxy_Custom_Proxy_URI_Vlidation_Failed_Message) ??
        "HTTP Proxy URI must be a http or https URI.";

    [ObservableProperty] public partial string ProxyUri { get; set; } = "";

    public HttpProxyModeItemViewModel SelectedHttpProxyMode
    {
        get => HttpProxyModes.First(x => x.Mode == appSettings.Value.HttpProxySettings);
        set
        {
            if (value.Mode == appSettings.Value.HttpProxySettings)
                return;

            OnPropertyChanging();
            OnProxyUriChanging(nameof(IsCustomProxySelected));
            appSettings.Update(settings => settings.HttpProxySettings = value.Mode);
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsCustomProxySelected));
        }
    }

    public bool IsCustomProxySelected => SelectedHttpProxyMode.Mode == AppHttpProxySettings.CustomProxy;

    public HttpProxyModeItemViewModel[] HttpProxyModes { get; } =
    [
        new(LangKeys.Pages_Settings_Http_Proxy_Proxy_Mode_Selector_Follow_System_Settings,
            AppHttpProxySettings.SystemProxy),
        new(LangKeys.Pages_Settings_Http_Proxy_Proxy_Mode_Selector_No_Proxy, AppHttpProxySettings.NoProxy),
        new(LangKeys.Pages_Settings_Http_Proxy_Proxy_Mode_Selector_Custom_Porxy, AppHttpProxySettings.CustomProxy)
    ];

    [RelayCommand]
    private void Load()
    {
        UpdateSettingsFromOptions();
    }

    private void UpdateSettingsFromOptions()
    {
        ProxyUri = appSettings.Value.HttpProxyUri?.ToString() ?? "";
    }

    [RelayCommand]
    private async Task ApplyHttpProxySettings()
    {
        if (!Uri.TryCreate(ProxyUri, UriKind.Absolute, out var uri) && IsCustomProxySelected)
            return;

        await appSettings.UpdateAsync(settings =>
        {
            settings.HttpProxyUri = uri;
            settings.HttpProxySettings = SelectedHttpProxyMode.Mode;
        });

        UpdateSettingsFromOptions();
    }
}

public record HttpProxyModeItemViewModel(string Name, AppHttpProxySettings Mode);