using Hmi.Application.Configuration;
using Hmi.Application.Messages;
using Hmi.Application.Services;
using Hmi.Domain.Devices;
using Hmi.Domain.Devices.Events;
using MediatR;
using Serilog;

namespace Hmi.Infrastructure.Communications;

/// <summary>
/// 采集宿主：PeriodicTimer 按绝对时间对齐不漂移；
/// 模块级状态机 + 指数退避重连；取消靠 CancellationToken，退出干净。
/// </summary>
public sealed class CollectionService(
    IConfigService config,
    IDeviceStore devices,
    IDataSource dataSource,
    IMediator mediator,
    IUiEventPublisher ui) : ICollectionService
{
    private CancellationTokenSource? _cts;
    private Task? _loop;

    public bool IsRunning => _loop is not null && !_loop.IsCompleted;

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (IsRunning)
        {
            return Task.CompletedTask;
        }
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _loop = Task.Run(() => RunAsync(_cts.Token), _cts.Token);
        ui.Publish(new StatusMessage("[采集] 已启动后台采集"));
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (_cts is null)
        {
            return;
        }
        _cts.Cancel();
        if (_loop is not null)
        {
            try { await _loop.ConfigureAwait(false); }
            catch (OperationCanceledException) { /* 正常退出 */ }
        }
        _cts.Dispose();
        _cts = null;
        _loop = null;
        ui.Publish(new ConnectionStateChangedMessage(false, "设备未连接"));
        ui.Publish(new StatusMessage("[采集] 已停止"));
    }

    private async Task RunAsync(CancellationToken ct)
    {
        var periodMs = Math.Max(50, config.Current.Collection.HeartbeatMs);
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(periodMs));

        while (await timer.WaitForNextTickAsync(ct))
        {
            try
            {
                var allDevices = devices.GetAll();
                foreach (var device in allDevices)
                {
                    foreach (var module in device.Modules)
                    {
                        if (module.RetryAfter.HasValue && DateTime.Now < module.RetryAfter.Value)
                            continue; // 指数退避中

                        if (module.Protocol != Protocol.MemorySim)
                        {
                            // S7 / Modbus 协议由插件式数据源实现，本期按假设 A5 暂不接入物理设备
                            module.State = CommunicationState.Offline;
                            continue;
                        }

                        try
                        {
                            var readings = await dataSource.ReadAsync(module.Points, ct);
                            module.State = CommunicationState.Connected;
                            module.RetryCount = 0;
                            module.RetryAfter = null;
                            module.LastConnectedAt = DateTime.Now;

                            foreach (var reading in readings)
                            {
                                await mediator.Publish(new DeviceDataCollectedEvent(
                                    device.Id, device.Name, module.Name,
                                    reading.TagCode, reading.Value, reading.Timestamp), ct);
                            }
                        }
                        catch (OperationCanceledException) { throw; }
                        catch (Exception ex)
                        {
                            module.RetryCount++;
                            module.State = CommunicationState.Reconnecting;
                            var delay = Math.Min(30_000, 500 * Math.Pow(2, Math.Min(module.RetryCount - 1, 6)));
                            module.RetryAfter = DateTime.Now.AddMilliseconds(delay);
                            Log.Warning(ex, "模块 {Module} 采集失败，第 {Retry} 次重连，{Delay}ms 后重试",
                                module.Name, module.RetryCount, delay);
                        }
                    }
                    device.IsOnline = device.OverallState is CommunicationState.Connected or CommunicationState.Partial;
                }

                var anyOnline = allDevices.Any(d => d.IsOnline);
                ui.Publish(new ConnectionStateChangedMessage(anyOnline, anyOnline ? "设备已连接" : "设备未连接"));
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                Log.Error(ex, "采集循环异常");
            }
        }
    }
}
