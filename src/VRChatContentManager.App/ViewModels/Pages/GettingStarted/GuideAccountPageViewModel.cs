using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DialogHostAvalonia;
using VRChatContentManager.App.Services;
using VRChatContentManager.App.ViewModels.Dialogs;
using VRChatContentManager.Core.Models.VRChatApi;
using VRChatContentManager.Core.Services.UserSession;

namespace VRChatContentManager.App.ViewModels.Pages.GettingStarted;

public sealed partial class GuideAccountPageViewModel(
    NavigationService navigationService,
    TwoFactorAuthDialogViewModelFactory twoFactorAuthDialogFactory,
    UserSessionManagerService userSessionManagerService,
    DialogService dialogService)
    : PageViewModelBase
{
    [ObservableProperty] public partial string Username { get; set; } = "";
    [ObservableProperty] public partial string Password { get; set; } = "";

    [RelayCommand]
    private void BackToWelcome()
    {
        navigationService.Navigate<GuideWelcomePageViewModel>();
    }

    [RelayCommand]
    private async Task Login()
    {
        var session = userSessionManagerService.CreateOrGetSession(Username);

        var loginResult = await session.LoginAsync(Password);

        if (loginResult.Requires2Fa.Length != 0 && (loginResult.Requires2Fa.Contains(Requires2FA.Totp) ||
                                                    loginResult.Requires2Fa.Contains(Requires2FA.EmailOtp)))
        {
            var isEmailOtp = loginResult.Requires2Fa.Contains(Requires2FA.EmailOtp);
            var result = await OpenTwoFactorAuthDialog(session, isEmailOtp);

            if (!result)
            {
                try
                {
                    await session.LogoutAsync();
                }
                catch
                {
                    // ignored
                }

                return;
            }
        }

        navigationService.Navigate<GuideSetupUnityPageViewModel>();
    }

    private async ValueTask<bool> OpenTwoFactorAuthDialog(UserSessionService userSessionService, bool isEmailOtp)
    {
        var dialog = twoFactorAuthDialogFactory.Create(userSessionService, isEmailOtp);

        var result = await dialogService.ShowDialogAsync(dialog);
        return result is true;
    }
}