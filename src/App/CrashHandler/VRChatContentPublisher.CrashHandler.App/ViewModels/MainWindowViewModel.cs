using CommunityToolkit.Mvvm.ComponentModel;

namespace VRChatContentPublisher.CrashHandler.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] public partial ViewModelBase? CurrentView { get; set; }
}