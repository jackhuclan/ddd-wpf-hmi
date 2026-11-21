namespace Hmi.Domain.WorkOrders;

/// <summary>工单实体：状态单向流转 Created → Assigned → Running → Completed</summary>
public sealed class WorkOrder
{
    public string Code { get; private set; } = string.Empty;
    public WorkOrderStatus Status { get; private set; }
    public Guid? DeviceId { get; private set; }
    public string? RecipeCode { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public string StatusText => Status switch
    {
        WorkOrderStatus.Assigned => "Assigned",
        WorkOrderStatus.Running => "Running",
        WorkOrderStatus.Completed => "Completed",
        _ => "Created"
    };

    private WorkOrder() { }

    public static WorkOrder Create(string? code = null) => new()
    {
        Code = string.IsNullOrWhiteSpace(code) ? $"WO-{DateTime.Now:yyyyMMddHHmmss}" : code.Trim(),
        Status = WorkOrderStatus.Created,
        CreatedAt = DateTime.Now
    };

    public void Assign(Guid deviceId, string? recipeCode)
    {
        if (Status != WorkOrderStatus.Created)
        {
            throw new InvalidOperationException("只有已创建的工单才能指派设备");
        }
        DeviceId = deviceId;
        RecipeCode = string.IsNullOrWhiteSpace(recipeCode) ? null : recipeCode.Trim();
        Status = WorkOrderStatus.Assigned;
    }

    public void Start()
    {
        if (Status != WorkOrderStatus.Assigned)
        {
            throw new InvalidOperationException("只有已指派的工单才能开始执行");
        }
        Status = WorkOrderStatus.Running;
    }

    public void Complete()
    {
        if (Status != WorkOrderStatus.Running)
        {
            throw new InvalidOperationException("只有运行中的工单才能完成归档");
        }
        Status = WorkOrderStatus.Completed;
    }
}
