using MediatR;

namespace Hmi.Domain.Recipes.Events;

/// <summary>配方下发结果事件：用于 UI 提示与审计</summary>
public sealed record RecipeDispatchEvent(
    string WorkOrderCode,
    string RecipeCode,
    string Action,
    string Reason) : INotification;
