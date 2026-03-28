using Antelcat.I18N.Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VRChatContentPublisher.App.Localization;
using VRChatContentPublisher.ConnectCore.Services.Connect;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;

namespace VRChatContentPublisher.App.ViewModels.Settings;

public sealed partial class ConnectSettingsViewModel(
    IWritableOptions<AppSettings> appSettings,
    HttpServerService httpServerService)
    : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsApplyEnabled))]
    [NotifyPropertyChangedFor(nameof(IsServerNameValid))]
    public partial string ConnectInstanceName { get; set; } = appSettings.Value.ConnectInstanceName;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPortInValidRange))]
    [NotifyPropertyChangedFor(nameof(ShowPortValidationError))]
    [NotifyPropertyChangedFor(nameof(IsApplyEnabled))]
    public partial string RpcServerPort { get; set; } = appSettings.Value.RpcServerPort.ToString();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string ErrorMessage { get; private set; } = "";

    public static string EmptyServerNameValidationErrorText =>
        I18NExtension.Translate(LangKeys.Pages_Settings_Connect_Apply_Error_Empty_Server_Name) ??
        "Server name are require.";

    public static string ServerPortOutOfRangeValidationErrorText =>
        string.Format(
            I18NExtension.Translate(
                LangKeys.Pages_Settings_Connect_Apply_Error_Port_Out_Of_Range) ?? "Port must be between {0} to {1}",
            MinUserPort, MaxUserPort);

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsApplyEnabled))]
    public partial bool IsSettingsModified { get; private set; }

    public bool IsServerNameValid => !string.IsNullOrWhiteSpace(ConnectInstanceName);
    public bool IsPortInValidRange => TryParseAndValidatePort(RpcServerPort, out _);
    public bool ShowPortValidationError => !IsPortInValidRange;
    public bool IsApplyEnabled => IsSettingsModified && IsPortInValidRange && IsServerNameValid;

    public static int MinUserPort => HttpServerService.MinUserPort;
    public static int MaxUserPort => HttpServerService.MaxUserPort;

    [RelayCommand]
    private async Task ApplyConnectSettings()
    {
        ClearError();

        if (!IsServerNameValid)
        {
            SetError(EmptyServerNameValidationErrorText);
            return;
        }

        if (!TryParseAndValidatePort(RpcServerPort, out var targetPort))
        {
            SetError(string.Format(ServerPortOutOfRangeValidationErrorText));

            return;
        }

        if (httpServerService.CurrentPort is { } activePort && activePort != targetPort)
        {
            var rebindResult = await httpServerService.RebindAsync(targetPort);
            if (!rebindResult.IsSuccess)
            {
                SetError(rebindResult.ErrorMessage ?? LangKeys.Pages_Settings_Connect_Apply_Error_Unknown_Error);
                return;
            }
        }

        await appSettings.UpdateAsync(settings =>
        {
            settings.ConnectInstanceName = ConnectInstanceName;
            settings.RpcServerPort = targetPort;
        });

        IsSettingsModified = false;
    }

    [RelayCommand]
    private void ResetToDefaultPort()
    {
        RpcServerPort = HttpServerService.DefaultPort.ToString();
    }

    partial void OnConnectInstanceNameChanged(string value)
    {
        MarkSettingsModified();
    }

    partial void OnRpcServerPortChanged(string value)
    {
        MarkSettingsModified();
    }

    private void MarkSettingsModified()
    {
        IsSettingsModified = true;
    }

    private static bool TryParseAndValidatePort(string value, out int port)
    {
        if (!int.TryParse(value, out port))
            return false;

        return port is >= HttpServerService.MinUserPort and <= HttpServerService.MaxUserPort;
    }

    private void SetError(string message)
    {
        ErrorMessage = message;
    }

    private void ClearError()
    {
        ErrorMessage = "";
    }
}