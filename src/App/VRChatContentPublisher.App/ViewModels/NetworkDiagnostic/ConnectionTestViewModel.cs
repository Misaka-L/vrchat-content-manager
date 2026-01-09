using System.Globalization;
using Avalonia.Data.Converters;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;

namespace VRChatContentPublisher.App.ViewModels.NetworkDiagnostic;

public sealed partial class ConnectionTestViewModel(
    string name,
    Func<ValueTask<string>> testFunc
)
    : ViewModelBase
{
    public string Name => name;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTaskRunning))]
    [NotifyPropertyChangedFor(nameof(IsTaskFailed))]
    [NotifyPropertyChangedFor(nameof(IsTaskSucceeded))]
    [NotifyPropertyChangedFor(nameof(IsTaskPending))]
    public partial NetworkDiagnosticTestStatus Status { get; private set; } = NetworkDiagnosticTestStatus.Pending;

    [ObservableProperty] public partial string TaskResult { get; private set; } = "";

    public bool IsTaskRunning => Status == NetworkDiagnosticTestStatus.Running;
    public bool IsTaskFailed => Status == NetworkDiagnosticTestStatus.Failed;
    public bool IsTaskSucceeded => Status == NetworkDiagnosticTestStatus.Succeeded;
    public bool IsTaskPending => Status == NetworkDiagnosticTestStatus.Pending;

    [RelayCommand]
    public async Task RunTestAsync()
    {
        Status = NetworkDiagnosticTestStatus.Running;

        try
        {
            var result = await testFunc();

            Status = NetworkDiagnosticTestStatus.Succeeded;
            TaskResult = result;
        }
        catch (Exception ex)
        {
            Status = NetworkDiagnosticTestStatus.Failed;
            var exceptionString = ex.ToString();

            TaskResult = exceptionString;
        }
    }

    public void ClearResult()
    {
        Status = NetworkDiagnosticTestStatus.Pending;
        TaskResult = "";
    }
}

public enum NetworkDiagnosticTestStatus
{
    Pending,
    Running,
    Succeeded,
    Failed
}

public sealed class NetworkDiagnosticTestStatusIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not NetworkDiagnosticTestStatus status)
            return null;

        switch (status)
        {
            case NetworkDiagnosticTestStatus.Pending:
                return MaterialIconKind.ProgressQuestion;
            case NetworkDiagnosticTestStatus.Running:
                return MaterialIconKind.ProgressClock;
            case NetworkDiagnosticTestStatus.Succeeded:
                return MaterialIconKind.Success;
            case NetworkDiagnosticTestStatus.Failed:
                return MaterialIconKind.GitIssue;
            default:
                return null;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}