using Hmi.Application.Configuration;
using Hmi.Application.Services;
using Hmi.Domain.Devices;

namespace Hmi.Infrastructure.Communications;

/// <summary>
/// 内存模拟数据源：产生围绕基准值波动的温度/压力。
/// 压力波动范围会周期性越过阈值，用于演示告警全生命周期。
/// </summary>
public sealed class MemorySimDataSource(IConfigService config) : IDataSource
{
    private readonly Random _random = new();

    public string Name => "内存模拟源";
    public Protocol Protocol => Protocol.MemorySim;
    public CommunicationState State => CommunicationState.Connected;

    public Task<IReadOnlyList<TagReading>> ReadAsync(
        IReadOnlyList<Tag> tags, CancellationToken cancellationToken)
    {
        var now = DateTime.Now;
        var readings = new List<TagReading>(tags.Count);

        foreach (var tag in tags)
        {
            cancellationToken.ThrowIfCancellationRequested();
            tag.WavePhase += 0.17;
            var wave = Math.Sin(tag.WavePhase) * 1.35 + (_random.NextDouble() - 0.5) * 0.5;

            var baseValue = tag.Code switch
            {
                "Temperature" => config.Current.Collection.TemperatureBase,
                "Pressure" => config.Current.Collection.PressureBase,
                _ => tag.Value > 0 ? tag.Value : 1.0
            };

            var value = Math.Round(Math.Max(0, baseValue + wave), 2);
            tag.Value = value;
            tag.Timestamp = now;
            readings.Add(new TagReading(tag.Code, value, now));
        }

        return Task.FromResult<IReadOnlyList<TagReading>>(readings);
    }
}
