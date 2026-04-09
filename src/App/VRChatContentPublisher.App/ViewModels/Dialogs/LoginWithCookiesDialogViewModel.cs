using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VRChatContentPublisher.Core.Services.UserSession;

namespace VRChatContentPublisher.App.ViewModels.Dialogs;

public sealed partial class LoginWithCookiesDialogViewModel(
    UserSessionManagerService userSessionManagerService
) : DialogViewModelBase
{
    public AvaloniaList<LoginWithCookiesDialogCookieItemViewModel> Cookies { get; } =
    [
        new("auth", "", false, true),
        new("twoFactorAuth", "", false, true)
    ];

    [ObservableProperty] public partial bool HasError { get; private set; }
    [ObservableProperty] public partial string ErrorMessage { get; private set; } = "";

    [RelayCommand]
    private void Load()
    {
        foreach (var cookie in Cookies)
        {
            cookie.OnRemovedRequested += OnCookieItemRequestRemoved;
        }
    }

    [RelayCommand]
    private void Unload()
    {
        foreach (var cookie in Cookies)
        {
            cookie.OnRemovedRequested -= OnCookieItemRequestRemoved;
        }
    }

    [RelayCommand]
    private void AddCookie()
    {
        var cookieItem = new LoginWithCookiesDialogCookieItemViewModel("", "", true);
        cookieItem.OnRemovedRequested += OnCookieItemRequestRemoved;
        Cookies.Add(cookieItem);
    }

    private void OnCookieItemRequestRemoved(object? sender, LoginWithCookiesDialogCookieItemViewModel e)
    {
        var cookieIndex = Cookies.IndexOf(e);
        if (cookieIndex > -1)
        {
            Cookies.RemoveAt(cookieIndex);
        }

        e.OnRemovedRequested -= OnCookieItemRequestRemoved;
    }

    [RelayCommand]
    private async Task Login()
    {
        HasError = false;
        ErrorMessage = "";

        var cookies = Cookies.ToDictionary(x => x.Name, x => x.Value);

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
}

public sealed class LoginWithCookiesDialogViewModelFactory(UserSessionManagerService userSessionManagerService)
{
    public LoginWithCookiesDialogViewModel Create()
    {
        return new LoginWithCookiesDialogViewModel(userSessionManagerService);
    }
}

public sealed partial class LoginWithCookiesDialogCookieItemViewModel(
    string name,
    string value,
    bool isRemovable,
    bool isKeyReadOnly = false
) : ViewModelBase
{
    [ObservableProperty] public partial string Name { get; set; } = name;
    [ObservableProperty] public partial string Value { get; set; } = value;

    [ObservableProperty] public partial bool IsKeyReadOnly { get; set; } = isKeyReadOnly;
    [ObservableProperty] public partial bool IsRemovable { get; set; } = isRemovable;

    public event EventHandler<LoginWithCookiesDialogCookieItemViewModel>? OnRemovedRequested;

    [RelayCommand]
    private void RequestRemove()
    {
        OnRemovedRequested?.Invoke(this, this);
    }

    [RelayCommand]
    private void SetValue(string value)
    {
        Value = value;
    }

    [RelayCommand]
    private void CLearValue()
    {
        Value = "";
    }
}