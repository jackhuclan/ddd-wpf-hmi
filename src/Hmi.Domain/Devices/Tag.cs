namespace Hmi.Domain.Devices;

/// <summary>点位：采集与告警的最小单位</summary>
public sealed class Tag
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public double Value { get; set; }
    public AccessMode AccessMode { get; set; } = AccessMode.Read;
    public DateTime Timestamp { get; set; }
    public double? UpperLimit { get; set; }
    public double? LowerLimit { get; set; }
    public double WavePhase { get; set; }

    public bool IsOutOfRange =>
        (UpperLimit.HasValue && Value > UpperLimit.Value) ||
        (LowerLimit.HasValue && Value < LowerLimit.Value);
}
