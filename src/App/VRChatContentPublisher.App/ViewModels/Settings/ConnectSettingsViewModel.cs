using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    public partial string ConnectInstanceName { get; set; } = appSettings.Value.ConnectInstanceName;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPortInValidRange))]
    [NotifyPropertyChangedFor(nameof(ShowPortValidationError))]
    public partial string RpcServerPort { get; set; } = appSettings.Value.RpcServerPort.ToString();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string ErrorMessage { get; private set; } = "";

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsApplyEnabled))]
    public partial bool IsSettingsModified { get; private set; }

    public bool IsPortInValidRange => TryParseAndValidatePort(RpcServerPort, out _);
    public bool ShowPortValidationError => !IsPortInValidRange;
    public bool IsApplyEnabled => IsSettingsModified && IsPortInValidRange;

    [RelayCommand]
    private async Task ApplyConnectSettings()
    {
        ClearError();

        if (!TryParseAndValidatePort(RpcServerPort, out var targetPort))
        {
            SetError($"Port must be between {HttpServerService.MinUserPort} and {HttpServerService.MaxUserPort}.");
            return;
        }

        if (httpServerService.CurrentPort is { } activePort && activePort != targetPort)
        {
            var rebindResult = await httpServerService.RebindAsync(targetPort);
            if (!rebindResult.IsSuccess)
            {
                SetError(rebindResult.ErrorMessage ?? "Failed to apply RPC server port.");
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

    [RelayCommand]
    private void FindAvailablePort()
    {
        var preferredPort = TryParseAndValidatePort(RpcServerPort, out var parsedPort)
            ? parsedPort
            : HttpServerService.DefaultPort;
        var availablePort = httpServerService.FindAvailablePort(preferredPort);
        if (availablePort is null)
        {
            SetError(
                $"No free port found in range {HttpServerService.MinUserPort}-{HttpServerService.MaxUserPort}.");
            return;
        }

        ClearError();
        RpcServerPort = availablePort.Value.ToString();
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