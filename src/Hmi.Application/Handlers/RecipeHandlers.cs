using Hmi.Application.Commands;
using Hmi.Application.Configuration;
using Hmi.Application.Messages;
using Hmi.Domain.Devices;
using Hmi.Domain.Recipes;
using Hmi.Domain.Recipes.Events;
using Hmi.Domain.WorkOrders.Events;
using MediatR;

namespace Hmi.Application.Handlers;

public sealed class ReloadConfigHandler(
    IConfigService config,
    IDeviceStore devices,
    IRecipeStore recipes,
    IUiEventPublisher ui) : IRequestHandler<ReloadConfigCommand, string>
{
    public Task<string> Handle(ReloadConfigCommand request, CancellationToken ct)
    {
        config.Reload();
        var cfg = config.Current;
        devices.ReplaceAll(cfg.Devices);
        recipes.ReplaceAll(cfg.Recipes);
        ui.Publish(new RecipeListChangedMessage());
        ui.Publish(new WorkOrderListChangedMessage());
        return Task.FromResult($"配置已重载：{cfg.Devices.Count} 台设备 / {cfg.Recipes.Count} 个配方 / {cfg.AlarmThresholds.Count} 组阈值");
    }
}

public sealed class SwitchRecipeHandler(IRecipeStore recipes, IUiEventPublisher ui)
    : IRequestHandler<SwitchRecipeCommand, string>
{
    public Task<string> Handle(SwitchRecipeCommand request, CancellationToken ct)
    {
        var recipe = recipes.GetByCode(request.Code) ?? throw new InvalidOperationException("配方不存在");
        recipes.SetEnabled(recipe.Code);
        ui.Publish(new RecipeListChangedMessage());
        return Task.FromResult($"配方 {recipe.Code} v{recipe.Version} 已切换下发");
    }
}

/// <summary>
/// 工单开始 → 配方下发判定：
/// 无配方跳过 / 取最新版 / 与设备当前版本比对 / 一致跳过、不一致切换
/// </summary>
public sealed class WorkOrderStartedRecipeHandler(
    IRecipeStore recipes,
    IDeviceStore devices,
    IUiEventPublisher ui,
    IMediator mediator) : INotificationHandler<WorkOrderStartedEvent>
{
    public async Task Handle(WorkOrderStartedEvent e, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(e.RecipeCode))
        {
            await mediator.Publish(new RecipeDispatchEvent(e.WorkOrderCode, string.Empty, "跳过", "工单未绑定配方"), ct);
            return;
        }

        var latest = recipes.GetLatest(e.RecipeCode);
        if (latest is null)
        {
            await mediator.Publish(new RecipeDispatchEvent(e.WorkOrderCode, e.RecipeCode, "跳过", "未找到配方配置"), ct);
            ui.Publish(new StatusMessage($"[配方] {e.RecipeCode} 未找到配置，跳过下发"));
            return;
        }

        var device = e.DeviceId.HasValue ? devices.GetById(e.DeviceId.Value) : null;
        if (device is not null &&
            string.Equals(device.CurrentRecipeVersion, latest.Version, StringComparison.OrdinalIgnoreCase))
        {
            await mediator.Publish(new RecipeDispatchEvent(e.WorkOrderCode, latest.Code, "跳过", "版本一致"), ct);
            ui.Publish(new StatusMessage($"[配方] 版本一致，跳过下发：{latest.Code} v{latest.Version}"));
            return;
        }

        recipes.SetEnabled(latest.Code);
        if (device is not null)
        {
            device.CurrentRecipeVersion = latest.Version;
            devices.Update(device);
        }
        ui.Publish(new RecipeListChangedMessage());
        await mediator.Publish(new RecipeDispatchEvent(e.WorkOrderCode, latest.Code, "切换下发", latest.Version), ct);
        ui.Publish(new StatusMessage($"[配方] 已切换下发：{latest.Code} v{latest.Version}"));
    }
}

/// <summary>配方事件只做状态栏提示，不产生业务副作用</summary>
public sealed class RecipeDispatchLogHandler(IUiEventPublisher ui) : INotificationHandler<RecipeDispatchEvent>
{
    public Task Handle(RecipeDispatchEvent e, CancellationToken ct)
    {
        ui.Publish(new StatusMessage($"[工单 {e.WorkOrderCode}] 配方 {e.RecipeCode}：{e.Action}（{e.Reason}）"));
        return Task.CompletedTask;
    }
}
