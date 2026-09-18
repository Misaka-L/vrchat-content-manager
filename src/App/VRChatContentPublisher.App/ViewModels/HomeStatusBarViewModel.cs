using System.Collections.Specialized;
using System.ComponentModel;
using VRChatContentPublisher.App.ViewModels.Data.PublishTasks;
using VRChatContentPublisher.App.ViewModels.Pages.HomeTab;
using VRChatContentPublisher.Core.Settings;
using VRChatContentPublisher.Core.Settings.Models;

namespace VRChatContentPublisher.App.ViewModels;

/// <summary>
/// Bottom status bar of the home page. Aggregates the task counts of every account task manager
/// and exposes the RPC server port.
/// </summary>
public sealed class HomeStatusBarViewModel : ViewModelBase
{
    private readonly HomeTasksPageViewModel _homeTasksPageViewModel;
    private readonly IWritableOptions<AppSettings> _appSettings;

    /// <summary>
    /// Containers currently subscribed to, mapped to the task manager instance currently exposed by
    /// them. Containers without a valid task manager (e.g. an invalid session) are not tracked and
    /// contribute nothing to the aggregated counts.
    /// </summary>
    private readonly Dictionary<PublishTaskManagerContainerViewModel, PublishTaskManagerViewModel>
        _managerSubscriptions = [];

    public HomeStatusBarViewModel(
        HomeTasksPageViewModel homeTasksPageViewModel,
        IWritableOptions<AppSettings> appSettings)
    {
        _homeTasksPageViewModel = homeTasksPageViewModel;
        _appSettings = appSettings;

        _homeTasksPageViewModel.TaskManagers.CollectionChanged += OnTaskManagersCollectionChanged;

        foreach (var container in _homeTasksPageViewModel.TaskManagers)
            Subscribe(container);
    }

    public string RpcServerPortText => _appSettings.Value.RpcServerPort.ToString();

    public int InProgressTaskCount => Aggregate(manager => manager.InProgressTaskCount);
    public int CompletedTaskCount => Aggregate(manager => manager.CompletedTaskCount);
    public int FailedTaskCount => Aggregate(manager => manager.FailedTaskCount);
    public int CanceledTaskCount => Aggregate(manager => manager.CanceledTaskCount);

    private int Aggregate(Func<PublishTaskManagerViewModel, int> selector) =>
        _managerSubscriptions.Values.Sum(selector);

    private void OnTaskManagersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (var item in e.OldItems)
            {
                if (item is PublishTaskManagerContainerViewModel container)
                    Unsubscribe(container);
            }
        }

        if (e.NewItems is not null)
        {
            foreach (var item in e.NewItems)
            {
                if (item is PublishTaskManagerContainerViewModel container)
                    Subscribe(container);
            }
        }

        NotifyTaskCountsChanged();
    }

    private void Subscribe(PublishTaskManagerContainerViewModel container)
    {
        container.PropertyChanged += OnContainerPropertyChanged;
        AddManagerSubscription(container);
    }

    private void Unsubscribe(PublishTaskManagerContainerViewModel container)
    {
        container.PropertyChanged -= OnContainerPropertyChanged;
        RemoveManagerSubscription(container);
    }

    private void OnContainerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(PublishTaskManagerContainerViewModel.PublishTaskManager))
            return;

        if (sender is not PublishTaskManagerContainerViewModel container)
            return;

        AddManagerSubscription(container);
        NotifyTaskCountsChanged();
    }

    private void AddManagerSubscription(PublishTaskManagerContainerViewModel container)
    {
        RemoveManagerSubscription(container);

        if (container.PublishTaskManager is not PublishTaskManagerViewModel manager)
            return;

        manager.PropertyChanged += OnManagerPropertyChanged;
        _managerSubscriptions[container] = manager;
    }

    private void RemoveManagerSubscription(PublishTaskManagerContainerViewModel container)
    {
        if (!_managerSubscriptions.Remove(container, out var manager))
            return;

        manager.PropertyChanged -= OnManagerPropertyChanged;
    }

    private void OnManagerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        NotifyTaskCountsChanged();
    }

    private void NotifyTaskCountsChanged()
    {
        OnPropertyChanged(nameof(InProgressTaskCount));
        OnPropertyChanged(nameof(CompletedTaskCount));
        OnPropertyChanged(nameof(FailedTaskCount));
        OnPropertyChanged(nameof(CanceledTaskCount));
    }
}
