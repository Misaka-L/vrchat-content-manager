using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VRChatContentPublisher.ConnectCore.Services.Connect;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;

namespace VRChatContentPublisher.App.ViewModels.Settings;

public sealed partial class ConnectSettingsViewModel : ViewModelBase
{
    [ObservableProperty] public partial string ConnectInstanceName { get; set; }
    [ObservableProperty] public partial string RpcServerPort { get; set; }

    [ObservableProperty] public partial bool HasError { get; private set; }
    [ObservableProperty] public partial string ErrorMessage { get; private set; } = "";
    [ObservableProperty] public partial string InfoMessage { get; private set; } = "";
    [ObservableProperty] public partial bool IsSettingsModified { get; private set; }

    private readonly IWritableOptions<AppSettings> _appSettings;
    private readonly HttpServerService _httpServerService;

    public bool IsPortInValidRange => TryParseAndValidatePort(RpcServerPort, out _);
    public bool ShowPortValidationError => !IsPortInValidRange;
    public bool IsApplyEnabled => IsSettingsModified && IsPortInValidRange;
    public bool HasInfoMessage => !string.IsNullOrWhiteSpace(InfoMessage);
    public string HostUri => $"http://localhost:{(TryParseAndValidatePort(RpcServerPort, out var port) ? port : "<invalid-port>")}";

    public ConnectSettingsViewModel(
        IWritableOptions<AppSettings> appSettings,
        HttpServerService httpServerService)
    {
        _appSettings = appSettings;
        _httpServerService = httpServerService;

        ConnectInstanceName = appSettings.Value.ConnectInstanceName;
        RpcServerPort = appSettings.Value.RpcServerPort.ToString();
    }

    [RelayCommand]
    private async Task ApplyConnectSettings()
    {
        ClearError();

        if (!TryParseAndValidatePort(RpcServerPort, out var targetPort))
        {
            SetError($"Port must be between {HttpServerService.MinUserPort} and {HttpServerService.MaxUserPort}.");
            return;
        }

        var previousPort = _httpServerService.CurrentPort;

        if (_httpServerService.CurrentPort is { } activePort && activePort != targetPort)
        {
            var rebindResult = await _httpServerService.RebindAsync(targetPort);
            if (!rebindResult.IsSuccess)
            {
                SetError(rebindResult.ErrorMessage ?? "Failed to apply RPC server port.");
                return;
            }
        }

        try
        {
            await _appSettings.UpdateAsync(settings =>
            {
                settings.ConnectInstanceName = ConnectInstanceName;
                settings.RpcServerPort = targetPort;
            });
        }
        catch (Exception ex)
        {
            if (previousPort is { } rollbackPort && _httpServerService.CurrentPort != rollbackPort)
            {
                await _httpServerService.RebindAsync(rollbackPort);
            }

            SetError($"Failed to save settings: {ex.Message}");
            return;
        }

        InfoMessage =
            $"Connect settings applied. Unity host is now {HostUri}. Update Unity Connect settings if needed.";
        IsSettingsModified = false;
    }

    [RelayCommand]
    private void ResetToDefaultPort()
    {
        RpcServerPort = HttpServerService.DefaultPort.ToString();
        InfoMessage = $"RPC port reset to default {HttpServerService.DefaultPort}.";
    }

    [RelayCommand]
    private void FindAvailablePort()
    {
        var preferredPort = TryParseAndValidatePort(RpcServerPort, out var parsedPort)
            ? parsedPort
            : HttpServerService.DefaultPort;
        var availablePort = _httpServerService.FindAvailablePort(preferredPort);
        if (availablePort is null)
        {
            SetError(
                $"No free port found in range {HttpServerService.MinUserPort}-{HttpServerService.MaxUserPort}.");
            return;
        }

        ClearError();
        RpcServerPort = availablePort.Value.ToString();
        InfoMessage = $"Found available port: {availablePort.Value}.";
    }

    partial void OnConnectInstanceNameChanged(string value)
    {
        MarkSettingsModified();
    }

    partial void OnRpcServerPortChanged(string value)
    {
        MarkSettingsModified();
        OnPropertyChanged(nameof(IsPortInValidRange));
        OnPropertyChanged(nameof(ShowPortValidationError));
        OnPropertyChanged(nameof(IsApplyEnabled));
        OnPropertyChanged(nameof(HostUri));
    }

    partial void OnInfoMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasInfoMessage));
    }

    partial void OnIsSettingsModifiedChanged(bool value)
    {
        OnPropertyChanged(nameof(IsApplyEnabled));
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
        InfoMessage = "";
        ErrorMessage = message;
        HasError = true;
    }

    private void ClearError()
    {
        ErrorMessage = "";
        HasError = false;
    }
}