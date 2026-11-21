using Hmi.Domain.Devices;
using Xunit;

namespace Hmi.Domain.Tests.Devices;

/// <summary>设备聚合：三级模型（设备 → 模块 → 点位）与整体通信状态判定。</summary>
public class DeviceTests
{
    private static CommunicationModule CreateModule(CommunicationState state) => new()
    {
        Name = "S7-1",
        State = state,
        Points = [new Tag { Code = "Temperature", Name = "温度" }]
    };

    [Fact]
    public void OverallState_without_modules_is_offline()
    {
        Assert.Equal(CommunicationState.Offline, new Device().OverallState);
    }

    [Fact]
    public void OverallState_with_all_connected_modules_is_connected()
    {
        var device = new Device { Modules = [CreateModule(CommunicationState.Connected), CreateModule(CommunicationState.Connected)] };

        Assert.Equal(CommunicationState.Connected, device.OverallState);
    }

    [Fact]
    public void OverallState_with_partial_modules_is_partial()
    {
        var device = new Device { Modules = [CreateModule(CommunicationState.Connected), CreateModule(CommunicationState.Offline)] };

        Assert.Equal(CommunicationState.Partial, device.OverallState);
    }

    [Fact]
    public void OverallState_with_reconnecting_module_is_partial()
    {
        var device = new Device { Modules = [CreateModule(CommunicationState.Reconnecting)] };

        Assert.Equal(CommunicationState.Partial, device.OverallState);
    }

    [Fact]
    public void OverallState_with_all_offline_modules_is_offline()
    {
        var device = new Device { Modules = [CreateModule(CommunicationState.Offline), CreateModule(CommunicationState.Connecting)] };

        Assert.Equal(CommunicationState.Offline, device.OverallState);
    }

    [Fact]
    public void AllTags_flattens_module_points()
    {
        var device = new Device
        {
            Modules =
            [
                CreateModule(CommunicationState.Connected),
                new CommunicationModule { Name = "Modbus-1", Points = [new Tag { Code = "Pressure" }] }
            ]
        };

        Assert.Equal(new[] { "Temperature", "Pressure" }, device.AllTags().Select(t => t.Code));
    }

    [Fact]
    public void Tag_out_of_range_checks_both_limits()
    {
        var tag = new Tag { Value = 5, UpperLimit = 4.5, LowerLimit = 1.0 };
        Assert.True(tag.IsOutOfRange);

        tag.Value = 0.5;
        Assert.True(tag.IsOutOfRange);

        tag.Value = 3.0;
        Assert.False(tag.IsOutOfRange);

        tag.LowerLimit = null;
        tag.Value = 5.5;
        Assert.True(tag.IsOutOfRange);
    }
}
