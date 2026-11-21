namespace Hmi.Domain.Devices;

/// <summary>通信模块：一台设备可挂 S7 / Modbus RTU / Modbus TCP 等多种协议模块</summary>
public sealed class CommunicationModule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DeviceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Protocol Protocol { get; set; } = Protocol.MemorySim;
    public CommunicationState State { get; set; } = CommunicationState.Offline;
    public int RetryCount { get; set; }
    /// <summary>指数退避：此时间之前跳过该模块的采集</summary>
    public DateTime? RetryAfter { get; set; }
    public DateTime? LastConnectedAt { get; set; }
    public List<Tag> Points { get; set; } = [];
}
