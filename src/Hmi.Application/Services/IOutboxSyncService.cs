namespace Hmi.Application.Services;

/// <summary>Outbox 队列：离线优先，告警先入本地队列，后台同步中心库</summary>
public interface IOutboxSyncService
{
    int PendingCount { get; }
    void Enqueue(string payload);
}
