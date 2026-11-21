using Hmi.Domain.Devices;
using Hmi.Domain.Recipes;

namespace Hmi.Application.Configuration;

/// <summary>appsettings.json 根配置。设备、配方、阈值均配置驱动。</summary>
public sealed class AppOptions
{
    public List<Device> Devices { get; set; } = [];
    public List<Recipe> Recipes { get; set; } = [];
    public Dictionary<string, ThresholdOptions> AlarmThresholds { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public CollectionOptions Collection { get; set; } = new();
    public SyncOptions Sync { get; set; } = new();
}

/// <summary>点位告警阈值配置</summary>
public sealed class ThresholdOptions
{
    public double Upper { get; set; }
    public double Lower { get; set; }
    public string Level { get; set; } = "Warning";
}

/// <summary>采集周期参数与模拟源基准值</summary>
public sealed class CollectionOptions
{
    public int HeartbeatMs { get; set; } = 500;
    public int MaxPoints { get; set; } = 600;
    public double TemperatureBase { get; set; } = 6.0;
    public double PressureBase { get; set; } = 4.36;
}

/// <summary>Outbox 后台同步周期</summary>
public sealed class SyncOptions
{
    public int IntervalSeconds { get; set; } = 30;
}

/// <summary>配置服务契约：支持热重载并广播最新配置</summary>
public interface IConfigService
{
    AppOptions Current { get; }
    void Reload();
}
