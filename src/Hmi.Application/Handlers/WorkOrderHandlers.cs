using Hmi.Application.Commands;
using Hmi.Application.Messages;
using Hmi.Domain.WorkOrders;
using Hmi.Domain.WorkOrders.Events;
using MediatR;

namespace Hmi.Application.Handlers;

public sealed class CreateWorkOrderHandler(IWorkOrderStore store, IUiEventPublisher ui)
    : IRequestHandler<CreateWorkOrderCommand, string>
{
    public Task<string> Handle(CreateWorkOrderCommand request, CancellationToken ct)
    {
        var order = WorkOrder.Create(request.Code);
        store.Add(order);
        ui.Publish(new WorkOrderListChangedMessage());
        return Task.FromResult($"工单 {order.Code} 已创建");
    }
}

public sealed class AssignWorkOrderHandler(IWorkOrderStore store, IUiEventPublisher ui)
    : IRequestHandler<AssignWorkOrderCommand, string>
{
    public Task<string> Handle(AssignWorkOrderCommand request, CancellationToken ct)
    {
        var order = store.GetByCode(request.Code) ?? throw new InvalidOperationException("工单不存在");
        order.Assign(request.DeviceId, request.RecipeCode);
        store.Update(order);
        ui.Publish(new WorkOrderListChangedMessage());
        return Task.FromResult($"工单 {order.Code} 已指派设备并绑定配方");
    }
}

public sealed class StartWorkOrderHandler(
    IWorkOrderStore store,
    IMediator mediator,
    IUiEventPublisher ui) : IRequestHandler<StartWorkOrderCommand, string>
{
    public async Task<string> Handle(StartWorkOrderCommand request, CancellationToken ct)
    {
        var order = store.GetByCode(request.Code) ?? throw new InvalidOperationException("工单不存在");
        order.Start();
        store.Update(order);
        await mediator.Publish(new WorkOrderStartedEvent(order.Code, order.DeviceId, order.RecipeCode), ct);
        ui.Publish(new WorkOrderListChangedMessage());
        return $"工单 {order.Code} 已开始执行";
    }
}

public sealed class CompleteWorkOrderHandler(IWorkOrderStore store, IUiEventPublisher ui)
    : IRequestHandler<CompleteWorkOrderCommand, string>
{
    public Task<string> Handle(CompleteWorkOrderCommand request, CancellationToken ct)
    {
        var order = store.GetByCode(request.Code) ?? throw new InvalidOperationException("工单不存在");
        order.Complete();
        store.Update(order);
        ui.Publish(new WorkOrderListChangedMessage());
        return Task.FromResult($"工单 {order.Code} 已完成归档");
    }
}
