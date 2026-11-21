namespace Hmi.Domain.WorkOrders;

/// <summary>工单状态，单向流转 Created → Assigned → Running → Completed</summary>
public enum WorkOrderStatus
{
    Created,
    Assigned,
    Running,
    Completed
}
