using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using VRChatContentPublisher.App.Services;
using VRChatContentPublisher.App.ViewModels.Dialogs;
using VRChatContentPublisher.Core.Models.VRChatApi;
using VRChatContentPublisher.Core.Services.UserSession;

namespace VRChatContentPublisher.App.ViewModels.Pages;

public sealed partial class AddAccountPageViewModel(
    ILogger<AddAccountPageViewModel> logger,
    TwoFactorAuthDialogViewModelFactory twoFactorAuthDialogFactory,
    UserSessionManagerService userSessionManagerService,
    DialogService dialogService,
    Action onRequestBack,
    Action onRequestDone)
    : PageViewModelBase
{
    [ObservableProperty] public partial string Username { get; set; } = "";
    [ObservableProperty] public partial string Password { get; set; } = "";

    [ObservableProperty] public partial bool HasError { get; private set; }
    [ObservableProperty] public partial string ErrorMessage { get; private set; } = "";

    [RelayCommand]
    private void Back()
    {
        onRequestBack();
    }

    [RelayCommand]
    private async Task Login()
    {
        if (userSessionManagerService.IsSessionExists(Username))
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

public sealed class AddAccountPageViewModelFactory(
    TwoFactorAuthDialogViewModelFactory twoFactorAuthDialogFactory,
    UserSessionManagerService userSessionManagerService,
    DialogService dialogService,
    ILogger<AddAccountPageViewModel> logger)
{
    public AddAccountPageViewModel Create(
        Action onRequestBack,
        Action onRequestDone)
    {
        return new AddAccountPageViewModel(
            logger,
            twoFactorAuthDialogFactory,
            userSessionManagerService,
            dialogService,
            onRequestBack,
            onRequestDone);
    }
}