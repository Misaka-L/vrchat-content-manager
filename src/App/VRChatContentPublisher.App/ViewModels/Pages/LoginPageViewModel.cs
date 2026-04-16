using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using VRChatContentPublisher.App.Services;
using VRChatContentPublisher.App.Services.Dialog;
using VRChatContentPublisher.App.ViewModels.Dialogs;
using VRChatContentPublisher.Core.Models.VRChatApi;
using VRChatContentPublisher.Core.Services.UserSession;

namespace VRChatContentPublisher.App.ViewModels.Pages;

public sealed partial class LoginPageViewModel(
    ILogger<LoginPageViewModel> logger,
    TwoFactorAuthDialogViewModelFactory twoFactorAuthDialogFactory,
    LoginWithCookiesDialogViewModelFactory loginWithCookiesDialogFactory,
    UserSessionManagerService userSessionManagerService,
    DialogService dialogService,
    Action onRequestBack,
    Action onRequestDone,
    string? username)
    : PageViewModelBase
{
    [ObservableProperty]
    public partial string Username { get; set; } = string.IsNullOrWhiteSpace(username) ? "" : username;

    public bool IsUsernameReadonly => !string.IsNullOrWhiteSpace(username);

    [ObservableProperty] public partial string Password { get; set; } = "";

    [ObservableProperty] public partial bool HasError { get; private set; }
    [ObservableProperty] public partial string ErrorMessage { get; private set; } = "";

    [RelayCommand]
    private void Back()
    {
        onRequestBack();
    }

    [RelayCommand]
    private async Task LoginWithCookies()
    {
        var dialogViewModel = loginWithCookiesDialogFactory.Create();
        var result = await dialogService.ShowDialogAsync(dialogViewModel);

        if (result is true)
        {
            onRequestDone();
        }
    }

    [RelayCommand]
    private async Task Login()
    {
        if (!IsUsernameReadonly && userSessionManagerService.IsSessionExists(Username))
        {
            logger.LogWarning("Attempt to add an existing account: {Username}", Username);
            SetError("An account with this username/email already exists.");
            return;
        }

        var session = userSessionManagerService.CreateOrGetSession(Username);

        LoginResult loginResult;
        try
        {
            loginResult = await session.LoginAsync(Password);
        }
        catch (ApiErrorException ex)
        {
            logger.LogError(ex, "Api error during login for user {Username}.", Username);
            await userSessionManagerService.RemoveSessionAsync(session);
            SetError(ex.ApiErrorMessage);
            return;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during login for user {Username}.", Username);
            await userSessionManagerService.RemoveSessionAsync(session);
            SetError(ex.Message);
            return;
        }

        ClearError();

        if (loginResult.Requires2Fa.Length != 0 && (loginResult.Requires2Fa.Contains(Requires2FA.Totp) ||
                                                    loginResult.Requires2Fa.Contains(Requires2FA.EmailOtp)))
        {
            var isEmailOtp = loginResult.Requires2Fa.Contains(Requires2FA.EmailOtp);
            var result = await OpenTwoFactorAuthDialog(session, isEmailOtp);

            if (!result)
            {
                try
                {
                    await userSessionManagerService.RemoveSessionAsync(session);
                }
                catch
                {
                    // ignored
                }

                return;
            }
        }

        try
        {
            await userSessionManagerService.HandleSessionAfterLogin(session);
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
            return;
        }

        onRequestDone();
    }

    private async ValueTask<bool> OpenTwoFactorAuthDialog(UserSessionService userSessionService, bool isEmailOtp)
    {
        var dialog = twoFactorAuthDialogFactory.Create(userSessionService, isEmailOtp);

        var result = await dialogService.ShowDialogAsync(dialog);
        return result is true;
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

public sealed class LoginPageViewModelFactory(
    TwoFactorAuthDialogViewModelFactory twoFactorAuthDialogFactory,
    LoginWithCookiesDialogViewModelFactory loginWithCookiesDialogFactory,
    UserSessionManagerService userSessionManagerService,
    DialogService dialogService,
    ILogger<LoginPageViewModel> logger)
{
    public LoginPageViewModel Create(
        Action onRequestBack,
        Action onRequestDone,
        string? username = null
    )
    {
        return new LoginPageViewModel(
            logger,
            twoFactorAuthDialogFactory,
            loginWithCookiesDialogFactory,
            userSessionManagerService,
            dialogService,
            onRequestBack,
            onRequestDone,
            username
        );
    }
}