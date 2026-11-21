using Hmi.Domain.WorkOrders;
using Xunit;

namespace Hmi.Domain.Tests.WorkOrders;

/// <summary>工单单向流转：Created → Assigned → Running → Completed，非法跃迁必须拦截。</summary>
public class WorkOrderTests
{
    [Fact]
    public void Create_generates_code_and_created_state()
    {
        var order = WorkOrder.Create("WO-TEST-001");

        Assert.Equal("WO-TEST-001", order.Code);
        Assert.Equal(WorkOrderStatus.Created, order.Status);
        Assert.Equal("Created", order.StatusText);
    }

    [Fact]
    public void Create_trims_code_and_generates_default_when_blank()
    {
        Assert.Equal("WO-A", WorkOrder.Create("  WO-A  ").Code);
        Assert.StartsWith("WO-", WorkOrder.Create("   ").Code);
    }

    [Fact]
    public void Assign_start_complete_follows_expected_transitions()
    {
        var order = WorkOrder.Create("WO-FLOW");

        order.Assign(Guid.NewGuid(), "RECIPE-A");
        Assert.Equal(WorkOrderStatus.Assigned, order.Status);
        Assert.Equal("RECIPE-A", order.RecipeCode);

        order.Start();
        Assert.Equal(WorkOrderStatus.Running, order.Status);

        order.Complete();
        Assert.Equal(WorkOrderStatus.Completed, order.Status);
    }

    [Fact]
    public void Assign_blank_recipe_code_is_normalized_to_null()
    {
        var order = WorkOrder.Create("WO-NO-RECIPE");

        order.Assign(Guid.NewGuid(), "   ");

        Assert.Null(order.RecipeCode);
    }

    [Fact]
    public void Illegal_transitions_throw()
    {
        var order = WorkOrder.Create("WO-ILLEGAL");

        Assert.Throws<InvalidOperationException>(() => order.Start());
        Assert.Throws<InvalidOperationException>(() => order.Complete());

        order.Assign(Guid.NewGuid(), null);
        Assert.Throws<InvalidOperationException>(() => order.Assign(Guid.NewGuid(), null));
        Assert.Throws<InvalidOperationException>(() => order.Complete());

        order.Start();
        Assert.Throws<InvalidOperationException>(() => order.Assign(Guid.NewGuid(), null));
    }
}
