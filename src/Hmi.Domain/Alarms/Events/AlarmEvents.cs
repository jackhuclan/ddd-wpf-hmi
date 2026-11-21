using MediatR;

namespace Hmi.Domain.Alarms.Events;

/// <summary>新告警产生事件</summary>
public sealed record AlarmRaisedEvent(Alarm Alarm) : INotification;

/// <summary>告警状态变化事件（如自动恢复）</summary>
public sealed record AlarmStateChangedEvent(Alarm Alarm) : INotification;
