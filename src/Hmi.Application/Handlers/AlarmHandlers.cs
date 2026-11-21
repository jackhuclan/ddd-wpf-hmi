using Hmi.Application.Commands;
using Hmi.Application.Configuration;
using Hmi.Application.Messages;
using Hmi.Application.Services;
using Hmi.Domain.Alarms;
using Hmi.Domain.Alarms.Events;
using Hmi.Domain.Devices.Events;
using MediatR;

namespace Hmi.Application.Handlers;

public sealed class AcknowledgeAlarmHandler(IAlarmStore store, IUiEventPublisher ui)
    : IRequestHandler<AcknowledgeAlarmCommand, string>
{
    public Task<string> Handle(AcknowledgeAlarmCommand request, CancellationToken ct)
    {
        var alarm = store.GetById(request.Id) ?? throw new InvalidOperationException("告警不存在");
        alarm.Acknowledge(); // 幂等
        ui.Publish(new AlarmListChangedMessage());
        return Task.FromResult($"告警 {alarm.Id.ToString("N")[..8]} 已确认");
    }
}

public sealed class ClearAlarmHandler(IAlarmStore store, IUiEventPublisher ui)
    : IRequestHandler<ClearAlarmCommand, string>
{
    public Task<string> Handle(ClearAlarmCommand request, CancellationToken ct)
    {
        var alarm = store.GetById(request.Id) ?? throw new InvalidOperationException("告警不存在");
        alarm.Clear(); // 未确认时抛 InvalidOperationException
        ui.Publish(new AlarmListChangedMessage());
        return Task.FromResult($"告警 {alarm.Id.ToString("N")[..8]} 已清除归档");
    }
}

public sealed class QueryAlarmsHandler(IAlarmStore store) : IRequestHandler<QueryAlarmsQuery, AlarmQueryResult>
{
    public Task<AlarmQueryResult> Handle(QueryAlarmsQuery request, CancellationToken ct)
    {
        var q = request.Query;
        IEnumerable<Alarm> items = store.GetAll();

        if (!string.IsNullOrWhiteSpace(q.Level) && q.Level != "全部")
        {
            items = items.Where(a => a.LevelText == q.Level);
        }
        if (!string.IsNullOrWhiteSpace(q.State) && q.State != "全部")
        {
            items = items.Where(a => a.StateText == q.State);
        }
        if (q.From.HasValue)
        {
            items = items.Where(a => a.RaisedAt >= q.From.Value.Date);
        }
        if (q.To.HasValue)
        {
            items = items.Where(a => a.RaisedAt < q.To.Value.Date.AddDays(1));
        }

        var all = items.OrderByDescending(a => a.RaisedAt).ToList();
        var pageCount = all.Count == 0 ? 0 : (int)Math.Ceiling(all.Count / (double)q.PageSize);
        var page = all.Skip((q.PageIndex - 1) * q.PageSize).Take(q.PageSize).ToList();
        return Task.FromResult(new AlarmQueryResult(page, all.Count, pageCount, q.PageIndex));
    }
}

/// <summary>越限判定 + 去重 + 自动恢复</summary>
public sealed class AlarmEvaluatorHandler(
    IConfigService config,
    IAlarmStore store,
    IMediator mediator,
    IUiEventPublisher ui,
    IOutboxSyncService outbox) : INotificationHandler<DeviceDataCollectedEvent>
{
    public async Task Handle(DeviceDataCollectedEvent e, CancellationToken ct)
    {
        var cfg = config.Current;
        if (!cfg.AlarmThresholds.TryGetValue(e.TagCode, out var threshold))
        {
            return;
        }

        var outOfRange = e.Value > threshold.Upper || e.Value < threshold.Lower;
        var source = $"Device:{e.DeviceId}";
        var active = store.GetActive().FirstOrDefault(a =>
            a.Source == source && a.PointCode == e.TagCode);

        if (outOfRange && active is null)
        {
            var level = threshold.Level switch
            {
                "Critical" => AlarmLevel.Critical,
                "Warning" => AlarmLevel.Warning,
                _ => AlarmLevel.Info
            };
            var unit = cfg.Devices.SelectMany(d => d.Modules)
                .SelectMany(m => m.Points)
                .FirstOrDefault(p => p.Code == e.TagCode)?.Unit ?? string.Empty;

            var alarm = Alarm.Raise(level, source, e.TagCode,
                $"工艺越限 {e.TagCode}={e.Value:0.00}{unit}", e.Value);
            store.Add(alarm);
            outbox.Enqueue($"ALARM|{alarm.Id:N}|{alarm.LevelText}|{alarm.Message}");
            await mediator.Publish(new AlarmRaisedEvent(alarm), ct);
            ui.Publish(new AlarmListChangedMessage());
            ui.Publish(new StatusMessage($"[告警] 新告警：{alarm.Message}"));
        }
        else if (!outOfRange && active is not null)
        {
            active.Recover();
            await mediator.Publish(new AlarmStateChangedEvent(active), ct);
            ui.Publish(new AlarmListChangedMessage());
            ui.Publish(new StatusMessage($"[恢复] 告警已自动恢复：{active.PointCode}"));
        }
    }
}

/// <summary>把领域事件转为 UI 事件：曲线推进</summary>
public sealed class ChartDataForwarderHandler(IUiEventPublisher ui)
    : INotificationHandler<DeviceDataCollectedEvent>
{
    private static int _sequence;

    public Task Handle(DeviceDataCollectedEvent e, CancellationToken ct)
    {
        var seq = Interlocked.Increment(ref _sequence);
        ui.Publish(new ChartDataPushedMessage(e.TagCode, e.Value, seq, e.Timestamp));
        return Task.CompletedTask;
    }
}
