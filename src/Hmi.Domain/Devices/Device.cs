namespace Hmi.Domain.Devices;

/// <summary>设备聚合根：设备 → 通信模块 → 点位</summary>
public sealed class Device
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    /// <summary>设备当前已下发的配方版本，用于工单开始时比对</summary>
    public string? CurrentRecipeVersion { get; set; }
    public List<CommunicationModule> Modules { get; set; } = [];

    public IEnumerable<Tag> AllTags() => Modules.SelectMany(m => m.Points);

    public CommunicationState OverallState
    {
        get
        {
            if (Modules.Count == 0)
            {
                return CommunicationState.Offline;
            }
            if (Modules.All(m => m.State == CommunicationState.Connected))
            {
                return CommunicationState.Connected;
            }
            if (Modules.Any(m => m.State is CommunicationState.Connected or CommunicationState.Reconnecting))
            {
                return CommunicationState.Partial;
            }
            return CommunicationState.Offline;
        }
    }
}
