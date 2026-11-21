namespace Hmi.Domain.WorkOrders;

/// <summary>工单仓储契约：按工单编号检索与更新</summary>
public interface IWorkOrderStore
{
    IReadOnlyList<WorkOrder> GetAll();
    WorkOrder? GetByCode(string code);
    void Add(WorkOrder workOrder);
    void Update(WorkOrder workOrder);
}
