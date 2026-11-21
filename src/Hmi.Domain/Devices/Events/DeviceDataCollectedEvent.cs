using MediatR;

namespace Hmi.Domain.Devices.Events;

/// <summary>采集到一个点位数据：设备上下文向其它上下文发布的领域事件</summary>
public sealed record DeviceDataCollectedEvent(
    Guid DeviceId,
    string DeviceName,
    string ModuleName,
    string TagCode,
    double Value,
    DateTime Timestamp) : INotification;
