using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using VRChatContentPublisher.App.Services;
using VRChatContentPublisher.App.ViewModels.Dialogs;
using VRChatContentPublisher.Core.Models.VRChatApi;
using VRChatContentPublisher.Core.Services.UserSession;

namespace VRChatContentPublisher.App.ViewModels.Pages.Settings;

public sealed partial class SettingsFixAccountPageViewModel(
    UserSessionService userSessionService,
    NavigationService navigationService,
    TwoFactorAuthDialogViewModelFactory twoFactorAuthDialogViewModelFactory,
    DialogService dialogService,
    ILogger<SettingsFixAccountPageViewModel> logger) : PageViewModelBase
{
    public string Username => userSessionService.UserNameOrEmail;
    [ObservableProperty] public partial string Password { get; set; } = "";

    [ObservableProperty] public partial bool HasError { get; private set; }
    [ObservableProperty] public partial string ErrorMessage { get; private set; } = "";

    [RelayCommand]
    private void BackToSettings()
    {
        navigationService.Navigate<SettingsPageViewModel>();
    }

    [RelayCommand]
    private async Task Login()
    {
        LoginResult loginResult;
        try
        {
            loginResult = await userSessionService.LoginAsync(Password);
        }
        catch (ApiErrorException ex)
        {
            logger.LogError(ex, "Api error during login for user {Username}.", Username);
            await TryLogoutAsync();
            SetError(ex.ApiErrorMessage);
            return;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during login for user {Username}.", Username);
            await TryLogoutAsync();
            SetError(ex.Message);
            return;
        }

        ClearError();

        if (loginResult.Requires2Fa.Length != 0 && (loginResult.Requires2Fa.Contains(Requires2FA.Totp) ||
                                                    loginResult.Requires2Fa.Contains(Requires2FA.EmailOtp)))
        {
            var isEmailOtp = loginResult.Requires2Fa.Contains(Requires2FA.EmailOtp);
            var result = await OpenTwoFactorAuthDialog(isEmailOtp);

            if (!result)
            {
                await TryLogoutAsync();

                return;
            }
        }

        try
        {
            await userSessionService.GetCurrentUserAsync();
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
            return;
        }

        navigationService.Navigate<SettingsPageViewModel>();
    }

    private async ValueTask<bool> OpenTwoFactorAuthDialog(bool isEmailOtp)
    {
        var dialog = twoFactorAuthDialogViewModelFactory.Create(userSessionService, isEmailOtp);

        var result = await dialogService.ShowDialogAsync(dialog);
        return result is true;
    }

    private async ValueTask TryLogoutAsync()
    {
        try
        {
            await userSessionService.LogoutAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to logout user session during error handling.");
        }
    }

    private void ClearError()
    {
        ErrorMessage = "";
        HasError = false;
    }

    private void SetError(string message)
    {
        ErrorMessage = message;
        HasError = true;
    }
}

public sealed class SettingsFixAccountPageViewModelFactory(
    NavigationService navigationService,
    TwoFactorAuthDialogViewModelFactory twoFactorAuthDialogViewModelFactory,
    DialogService dialogService,
    ILogger<SettingsFixAccountPageViewModel> logger)
{
    public SettingsFixAccountPageViewModel Create(UserSessionService userSessionService)
    {
        return new SettingsFixAccountPageViewModel(userSessionService,
            navigationService,
            twoFactorAuthDialogViewModelFactory,
            dialogService,
            logger);
    }
}