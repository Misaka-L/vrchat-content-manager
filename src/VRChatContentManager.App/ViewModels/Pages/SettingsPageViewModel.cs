﻿using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VRChatContentManager.App.Services;
using VRChatContentManager.App.ViewModels.Data;
using VRChatContentManager.App.ViewModels.Pages.Settings;
using VRChatContentManager.App.ViewModels.Settings;
using VRChatContentManager.Core.Services.UserSession;
using VRChatContentManager.Core.Settings;
using VRChatContentManager.Core.Settings.Models;

namespace VRChatContentManager.App.ViewModels.Pages;

public sealed partial class SettingsPageViewModel(
    NavigationService navigationService,
    AccountsSettingsViewModel accountsSettingsViewModel,
    ConnectSettingsViewModel connectSettingsViewModel) : PageViewModelBase
{
    public AccountsSettingsViewModel AccountsSettingsViewModel { get; } = accountsSettingsViewModel;
    public ConnectSettingsViewModel ConnectSettingsViewModel { get; } = connectSettingsViewModel;
    
    [RelayCommand]
    private void NavigateToHome()
    {
        navigationService.Navigate<HomePageViewModel>();
    }
}