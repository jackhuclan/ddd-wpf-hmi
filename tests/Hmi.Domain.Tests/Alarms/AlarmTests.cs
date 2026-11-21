using Hmi.Domain.Alarms;
using Xunit;

namespace Hmi.Domain.Tests.Alarms;

/// <summary>告警状态机：确认与恢复相互独立，清除必须先确认（需求 F3 / 约束 C2）。</summary>
public class AlarmTests
{
    private static Alarm CreateAlarm() => Alarm.Raise(
        AlarmLevel.Warning, "Device:1", "Pressure", "工艺越限 Pressure=4.80MPa", 4.80);

    [Fact]
    public void Raise_initial_state_is_unacknowledged()
    {
        var alarm = CreateAlarm();

        Assert.Equal(AlarmState.Unacknowledged, alarm.State);
        Assert.Equal("未确认", alarm.StateText);
        Assert.False(alarm.IsRecovered);
        Assert.False(alarm.IsCleared);
    }

    [Fact]
    public void Acknowledge_is_idempotent_and_records_timestamp()
    {
        var alarm = CreateAlarm();

        alarm.Acknowledge();
        var first = alarm.AcknowledgedAt;
        alarm.Acknowledge();

        Assert.True(alarm.IsAcknowledged);
        Assert.Equal(first, alarm.AcknowledgedAt);
        Assert.Equal(AlarmState.Acknowledged, alarm.State);
    }

    [Fact]
    public void Recover_does_not_overwrite_acknowledgement()
    {
        var alarm = CreateAlarm();
        alarm.Acknowledge();

        alarm.Recover();

        Assert.True(alarm.IsAcknowledged);
        Assert.True(alarm.IsRecovered);
        Assert.Equal(AlarmState.Recovered, alarm.State);
        Assert.Equal("已恢复", alarm.StateText);
    }

    [Fact]
    public void Clear_without_acknowledge_throws()
    {
        var alarm = CreateAlarm();

        var ex = Assert.Throws<InvalidOperationException>(() => alarm.Clear());

        Assert.Contains("必须先确认", ex.Message);
        Assert.False(alarm.IsCleared);
    }

    [Fact]
    public void Clear_after_acknowledge_succeeds()
    {
        var alarm = CreateAlarm();
        alarm.Acknowledge();

        alarm.Clear();
        alarm.Clear(); // 重复清除幂等

        Assert.True(alarm.IsCleared);
        Assert.NotNull(alarm.ClearedAt);
        Assert.Equal(AlarmState.Cleared, alarm.State);
    }
}
