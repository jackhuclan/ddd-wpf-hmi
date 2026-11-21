using Hmi.Domain.Devices;

namespace Hmi.Application.Services;

/// <summary>采集宿主接口：PeriodicTimer + CancellationToken，启停干净</summary>
public interface ICollectionService
{
    bool IsRunning { get; }
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync();
}

/// <summary>插件式数据源接口：新增协议只需新增实现并注册</summary>
public interface IDataSource
{
    string Name { get; }
    Protocol Protocol { get; }
    CommunicationState State { get; }
    Task<IReadOnlyList<TagReading>> ReadAsync(
        IReadOnlyList<Tag> tags,
        CancellationToken cancellationToken);
}

/// <summary>一次点位采集结果</summary>
public sealed record TagReading(string TagCode, double Value, DateTime Timestamp);
