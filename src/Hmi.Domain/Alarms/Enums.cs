namespace Hmi.Domain.Alarms;

/// <summary>告警级别</summary>
public enum AlarmLevel
{
    Info,
    Warning,
    Critical
}

/// <summary>告警生命周期状态（值顺序不可调换）</summary>
public enum AlarmState
{
    Unacknowledged,
    Acknowledged,
    Recovered,
    Cleared
}
