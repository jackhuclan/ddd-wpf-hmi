using MediatR;

namespace Hmi.Domain.WorkOrders.Events;

/// <summary>工单开始事件，携带配方编号，触发配方下发判定</summary>
public sealed record WorkOrderStartedEvent(
    string WorkOrderCode,
    Guid? DeviceId,
    string? RecipeCode) : INotification;
