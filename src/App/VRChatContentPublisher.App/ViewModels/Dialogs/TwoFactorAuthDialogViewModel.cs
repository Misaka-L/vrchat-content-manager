using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using VRChatContentPublisher.Core.Models.VRChatApi;
using VRChatContentPublisher.Core.Services.UserSession;

namespace VRChatContentPublisher.App.ViewModels.Dialogs;

public sealed partial class TwoFactorAuthDialogViewModel(
    ILogger<TwoFactorAuthDialogViewModel> logger,
    UserSessionService userSessionService,
    bool isEmailOtp)
    : DialogViewModelBase
{
    [ObservableProperty] public partial string Code { get; set; } = "";
    public bool IsEmailOtp => isEmailOtp;

    [ObservableProperty] public partial bool HasError { get; private set; }
    [ObservableProperty] public partial string ErrorMessage { get; private set; } = "";

    [RelayCommand]
    private async Task Verify()
    {
        var apiClient = userSessionService.GetApiClient();

        bool isVerify;
        try
        {
            isVerify = await apiClient.VerifyOtpAsync(Code, IsEmailOtp);
        }
        catch (ApiErrorException ex)
        {
            logger.LogError(ex, "Failed to verify OTP code.");
            HasError = true;
            ErrorMessage = ex.ApiErrorMessage;
            return;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to verify OTP code.");
            HasError = true;
            ErrorMessage = ex.Message;
            return;
        }

        if (!isVerify)
        {
            logger.LogError("Invalid OTP code.");
            HasError = true;
            ErrorMessage = "Invalid Code";
            return;
        }

        HasError = false;

        RequestClose(true);
    }
}

public sealed class TwoFactorAuthDialogViewModelFactory(ILogger<TwoFactorAuthDialogViewModel> logger)
{
    public TwoFactorAuthDialogViewModel Create(UserSessionService userSessionService, bool isEmailOtp)
    {
        return new TwoFactorAuthDialogViewModel(logger, userSessionService, isEmailOtp);
    }
}