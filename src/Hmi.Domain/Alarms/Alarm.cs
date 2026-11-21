namespace Hmi.Domain.Alarms;

/// <summary>
/// 告警实体：承担领域规则。
/// 确认与恢复相互独立；清除必须先确认，由实体强制。
/// </summary>
public sealed class Alarm
{
    public Guid Id { get; private set; }
    public AlarmLevel Level { get; private set; }
    public string Source { get; private set; } = string.Empty;
    public string PointCode { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public double Value { get; private set; }
    public DateTime RaisedAt { get; private set; }

    public bool IsAcknowledged { get; private set; }
    public bool IsRecovered { get; private set; }
    public bool IsCleared { get; private set; }
    public DateTime? AcknowledgedAt { get; private set; }
    public DateTime? RecoveredAt { get; private set; }
    public DateTime? ClearedAt { get; private set; }

    public AlarmState State =>
        IsCleared ? AlarmState.Cleared :
        IsRecovered ? AlarmState.Recovered :
        IsAcknowledged ? AlarmState.Acknowledged :
        AlarmState.Unacknowledged;

    public string StateText => State switch
    {
        AlarmState.Cleared => "已清除",
        AlarmState.Recovered => "已恢复",
        AlarmState.Acknowledged => "已确认",
        _ => "未确认"
    };

    public string LevelText => Level switch
    {
        AlarmLevel.Critical => "Critical",
        AlarmLevel.Warning => "Warning",
        _ => "Info"
    };

    private Alarm() { }

    public static Alarm Raise(AlarmLevel level, string source, string pointCode,
        string message, double value, DateTime? raisedAt = null) => new()
    {
        Id = Guid.NewGuid(),
        Level = level,
        Source = source,
        PointCode = pointCode,
        Message = message,
        Value = value,
        RaisedAt = raisedAt ?? DateTime.Now
    };

    /// <summary>确认：幂等，标记值班已知悉</summary>
    public void Acknowledge()
    {
        if (IsAcknowledged)
        {
            return;
        }
        IsAcknowledged = true;
        AcknowledgedAt = DateTime.Now;
    }

    /// <summary>自动恢复：与确认相互独立，不改写 IsAcknowledged</summary>
    public void Recover()
    {
        if (IsRecovered)
        {
            return;
        }
        IsRecovered = true;
        RecoveredAt = DateTime.Now;
    }

    /// <summary>清除归档：必须先确认，否则抛异常</summary>
    public void Clear()
    {
        if (!IsAcknowledged)
        {
            throw new InvalidOperationException("告警必须先确认才能清除归档");
        }
        if (IsCleared)
        {
            return;
        }
        IsCleared = true;
        ClearedAt = DateTime.Now;
    }
}
