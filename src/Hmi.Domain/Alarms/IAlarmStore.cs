namespace Hmi.Domain.Alarms;

/// <summary>告警仓储契约：活动列表排除已恢复与已清除项</summary>
public interface IAlarmStore
{
    void Add(Alarm alarm);
    IReadOnlyList<Alarm> GetAll();
    Alarm? GetById(Guid id);
    /// <summary>活动告警：未清除且未恢复（值回落自动从活动列表消失）</summary>
    IReadOnlyList<Alarm> GetActive();
}
