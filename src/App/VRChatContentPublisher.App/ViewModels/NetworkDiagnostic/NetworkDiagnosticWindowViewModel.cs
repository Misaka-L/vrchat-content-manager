using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VRChatContentPublisher.Core.Services.VRChatApi;

namespace VRChatContentPublisher.App.ViewModels.NetworkDiagnostic;

public sealed partial class NetworkDiagnosticWindowViewModel(
    VRChatApiDiagnosticService diagnosticService
) : ViewModelBase
{
    public AvaloniaList<StatusPageComponentViewModel> StatusPageComponents { get; } = [];
    [ObservableProperty] public partial string StatusSummary { get; private set; } = "";

    public AvaloniaList<ConnectionTestViewModel> ConnectionTests { get; } =
    [
        new(
            "Send test request to VRChat API",
            async () => await diagnosticService.SendTestApiRequestAsync()
        ),
        new(
            "Connect to s3.us-east-1.amazonaws.com",
            async () =>
            {
                await diagnosticService.TestAwsS3ConnectionAsync();
                return "Test succeeded.";
            }
        ),
        new(
            "Connect to www.cloudflare.com",
            async () => await diagnosticService.GetCloudflareTraceAsync()
        ),
        new(
            "Connect to www.cloudflare-cn.com",
            async () => await diagnosticService.GetCloudflareChinaTraceAsync()
        )
    ];

    [ObservableProperty] public partial string CloudflareTraceResult { get; private set; } = "";

    [RelayCommand]
    private async Task RunDiagnosticAsync()
    {
        CloudflareTraceResult = "Running...";

        foreach (var test in ConnectionTests)
        {
            test.ClearResult();
        }

        var tasksToRun = ConnectionTests
            .Select(test => test.RunTestAsync())
            .ToList();
        tasksToRun.Add(RunCloudflareTraceTestAsync());
        tasksToRun.Add(GetStatusSummaryAsync());

        await Task.WhenAll(tasksToRun);
    }

    private async Task GetStatusSummaryAsync()
    {
        try
        {
            StatusPageComponents.Clear();

            var summary = await diagnosticService.GetApiStatusSummaryAsync();
            var components = new List<StatusPageComponentViewModel>();

            StatusSummary = summary.Status.Indicator + " - " + summary.Status.Description;
            foreach (var summaryComponent in summary.Components.OrderByDescending(x => x.IsGroup))
            {
                var viewModel = new StatusPageComponentViewModel(
                    summaryComponent.Id,
                    summaryComponent.Name,
                    summaryComponent.Status);

                if (summaryComponent.GroupId is not { } groupId)
                {
                    components.Add(viewModel);
                    continue;
                }

                if (components.FirstOrDefault(x => x.Id == summaryComponent.GroupId) is not { } groupComponent)
                {
                    components.Add(viewModel);
                    continue;
                }

                groupComponent.SubComponents.Add(viewModel);
            }

            StatusPageComponents.AddRange(components);
        }
        catch (Exception ex)
        {
        }
    }

    private async Task RunCloudflareTraceTestAsync()
    {
        try
        {
            CloudflareTraceResult = await diagnosticService.GetApiCloudflareTraceAsync();
        }
        catch (Exception ex)
        {
            CloudflareTraceResult = ex.ToString();
        }
    }
}