using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VRChatContentPublisher.Core.Services.UserSession;

namespace VRChatContentPublisher.App.ViewModels.Dialogs;

public sealed partial class LoginWithCookiesDialogViewModel(
    UserSessionManagerService userSessionManagerService
) : DialogViewModelBase
{
    [ObservableProperty] public partial string CookiesText { get; set; } = "";

    [ObservableProperty] public partial bool HasError { get; private set; }
    [ObservableProperty] public partial string ErrorMessage { get; private set; } = "";

    [RelayCommand]
    private async Task Login()
    {
        HasError = false;
        ErrorMessage = "";

        if (TryParseCookiesText() is not { } cookies)
            return;

        try
        {
            await userSessionManagerService.CreateOrGetSessionFromCookiesAsync(cookies);
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            return;
        }

        RequestClose(true);
    }

    private Dictionary<string, string>? TryParseCookiesText()
    {
        var lines = CookiesText.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        var cookies = new Dictionary<string, string>();
        foreach (var line in lines)
        {
            var linePart = line.Split("=", 2);
            if (linePart.Length != 2)
            {
                HasError = true;
                ErrorMessage = "Invalid cookie format. Each line should be in the format 'key=value'.";
                return null;
            }

            cookies.Add(linePart[0], linePart[1]);
        }

        return cookies;
    }
}

public sealed class LoginWithCookiesDialogViewModelFactory(UserSessionManagerService userSessionManagerService)
{
    public LoginWithCookiesDialogViewModel Create()
    {
        return new LoginWithCookiesDialogViewModel(userSessionManagerService);
    }
}