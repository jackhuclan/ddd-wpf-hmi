using System.Collections.Concurrent;
using Hmi.Application.Configuration;
using Hmi.Application.Services;
using Serilog;

namespace Hmi.Infrastructure.Sync;

/// <summary>
/// Outbox 内存实现：告警先入本地队列，后台按配置周期"同步"到中心库。
/// 无中心 SQL Server 环境时以日志模拟（需求假设 A6），接口可直接替换为 EF Core 实现。
/// </summary>
public sealed class OutboxSyncService : IOutboxSyncService, IDisposable
{
    private readonly ConcurrentQueue<string> _queue = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _worker;

    public OutboxSyncService(IConfigService config)
    {
        _worker = Task.Run(() => RunAsync(config, _cts.Token));
    }

    public int PendingCount => _queue.Count;

    public void Enqueue(string payload) => _queue.Enqueue(payload);

    private async Task RunAsync(IConfigService config, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var seconds = Math.Max(1, config.Current.Sync.IntervalSeconds);
                await Task.Delay(TimeSpan.FromSeconds(seconds), ct);
                var count = 0;
                while (_queue.TryDequeue(out var payload))
                {
                    count++;
                    Log.Information("Outbox → 中心库: {Payload}", payload);
                }
                if (count > 0)
                {
                    Log.Information("Outbox 同步完成，共 {Count} 条", count);
                }
            }
        }
        catch (OperationCanceledException) { /* 正常退出 */ }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
