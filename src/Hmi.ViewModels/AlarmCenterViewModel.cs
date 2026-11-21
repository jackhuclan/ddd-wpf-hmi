using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Hmi.Application.Commands;
using Hmi.Application.Messages;
using Hmi.Domain.Alarms;
using MediatR;

namespace Hmi.ViewModels;

/// <summary>告警列表行视图模型</summary>
public sealed class AlarmRow
{
    public Alarm Alarm { get; }
    public AlarmRow(Alarm alarm) => Alarm = alarm;

    public string LevelText => Alarm.LevelText;
    public string Source => Alarm.Source;
    public string PointCode => Alarm.PointCode;
    public string Message => Alarm.Message;
    public string StateText => Alarm.StateText;
    public string TimeText => Alarm.RaisedAt.ToString("HH:mm:ss");
    public string FullTimeText => Alarm.RaisedAt.ToString("yyyy-MM-dd HH:mm:ss");
    public bool IsAcknowledged => Alarm.IsAcknowledged;
}

/// <summary>告警中心页：活动告警确认 / 清除与历史告警分页查询</summary>
public sealed partial class AlarmCenterViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly IAlarmStore _store;
    private readonly IMessenger _messenger;

    public override string PageTitle => "告警中心";

    public ObservableCollection<AlarmRow> ActiveAlarms { get; } = [];
    public ObservableCollection<AlarmRow> HistoryAlarms { get; } = [];
    public ObservableCollection<string> LevelOptions { get; } = ["全部", "Info", "Warning", "Critical"];
    public ObservableCollection<string> StateOptions { get; } = ["全部", "未确认", "已确认", "已恢复", "已清除"];

    [ObservableProperty] private AlarmRow? _selectedActive;
    [ObservableProperty] private string _selectedLevel = "全部";
    [ObservableProperty] private string _selectedState = "全部";
    [ObservableProperty] private DateTime? _fromDate = DateTime.Today.AddDays(-7);
    [ObservableProperty] private DateTime? _toDate = DateTime.Today.AddDays(1);
    [ObservableProperty] private int _pageIndex = 1;
    [ObservableProperty] private int _pageCount;
    [ObservableProperty] private int _totalCount;

    public string PageText => $"{PageIndex} / {PageCount}";
    public string TotalText => $"共 {TotalCount} 条";

    public IAsyncRelayCommand AcknowledgeCommand { get; }
    public IAsyncRelayCommand ClearCommand { get; }
    public IAsyncRelayCommand QueryCommand { get; }
    public IRelayCommand PrevPageCommand { get; }
    public IRelayCommand NextPageCommand { get; }

    public AlarmCenterViewModel(IMediator mediator, IAlarmStore store, IMessenger messenger)
    {
        _mediator = mediator;
        _store = store;
        _messenger = messenger;

        AcknowledgeCommand = new AsyncRelayCommand(AcknowledgeAsync, CanAcknowledge);
        ClearCommand = new AsyncRelayCommand(ClearAsync, CanClear);
        QueryCommand = new AsyncRelayCommand(QueryAsync);
        PrevPageCommand = new RelayCommand(() => { if (PageIndex > 1) { PageIndex--; _ = QueryAsync(); } }, () => PageIndex > 1);
        NextPageCommand = new RelayCommand(() => { if (PageIndex < PageCount) { PageIndex++; _ = QueryAsync(); } }, () => PageIndex < PageCount);

        messenger.Register<AlarmListChangedMessage, string>(this, HmiMessages.Token, (_, _) =>
        {
            RefreshActive();
            _ = QueryAsync();
        });

        RefreshActive();
        _ = QueryAsync();
    }

    partial void OnSelectedActiveChanged(AlarmRow? value)
    {
        AcknowledgeCommand.NotifyCanExecuteChanged();
        ClearCommand.NotifyCanExecuteChanged();
    }

    private bool CanAcknowledge() => SelectedActive is { IsAcknowledged: false };

    // 未确认的清除按钮是灰的（领域规则前置拦截）
    private bool CanClear() => SelectedActive is { IsAcknowledged: true };

    private void RefreshActive()
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            dispatcher.Invoke(RefreshActive);
            return;
        }

        ActiveAlarms.Clear();
        foreach (var alarm in _store.GetActive().OrderByDescending(a => a.RaisedAt))
            ActiveAlarms.Add(new AlarmRow(alarm));

        if (SelectedActive is not null &&
            !ActiveAlarms.Any(r => ReferenceEquals(r.Alarm, SelectedActive.Alarm)))
            SelectedActive = null;

        AcknowledgeCommand.NotifyCanExecuteChanged();
        ClearCommand.NotifyCanExecuteChanged();
    }

    private async Task AcknowledgeAsync()
    {
        if (SelectedActive is null)
        {
            return;
        }
        var tip = await _mediator.Send(new AcknowledgeAlarmCommand(SelectedActive.Alarm.Id));
        _messenger.Send(new StatusMessage(tip), HmiMessages.Token);
        RefreshActive();
    }

    private async Task ClearAsync()
    {
        if (SelectedActive is null)
        {
            return;
        }
        var tip = await _mediator.Send(new ClearAlarmCommand(SelectedActive.Alarm.Id));
        _messenger.Send(new StatusMessage(tip), HmiMessages.Token);
        RefreshActive();
        await QueryAsync();
    }

    private async Task QueryAsync()
    {
        var result = await _mediator.Send(new QueryAlarmsQuery(
            new AlarmQuery(SelectedLevel, SelectedState, FromDate, ToDate, PageIndex, 20)));

        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            dispatcher.Invoke(() => ApplyQuery(result));
            return;
        }
        ApplyQuery(result);
    }

    private void ApplyQuery(AlarmQueryResult result)
    {
        HistoryAlarms.Clear();
        foreach (var alarm in result.Items) HistoryAlarms.Add(new AlarmRow(alarm));
        TotalCount = result.TotalCount;
        PageCount = result.PageCount;
        OnPropertyChanged(nameof(PageText));
        OnPropertyChanged(nameof(TotalText));
        PrevPageCommand.NotifyCanExecuteChanged();
        NextPageCommand.NotifyCanExecuteChanged();
    }
}
