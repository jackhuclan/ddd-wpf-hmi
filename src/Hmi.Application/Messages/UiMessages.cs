namespace Hmi.Application.Messages;

/// <summary>领域事件转发到 UI 的消息（对应原文的 ChartDataPushedEvent）</summary>
public sealed record ChartDataPushedMessage(
    string TagCode,
    double Value,
    int Sequence,
    DateTime Timestamp);

/// <summary>告警列表变更通知</summary>
public sealed record AlarmListChangedMessage;

/// <summary>工单列表变更通知</summary>
public sealed record WorkOrderListChangedMessage;

/// <summary>配方列表变更通知</summary>
public sealed record RecipeListChangedMessage;

/// <summary>设备连接状态变更通知</summary>
public sealed record ConnectionStateChangedMessage(bool IsConnected, string Text);

/// <summary>底部状态栏提示</summary>
public sealed record StatusMessage(string Text);

/// <summary>UI 事件发布器：领域处理器不直接依赖 WPF / Prism</summary>
public interface IUiEventPublisher
{
    void Publish<T>(T message) where T : class;
}
