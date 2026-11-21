using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Hmi.Application.Messages;
using Hmi.Application.Services;

namespace Hmi.ViewModels;

/// <summary>曲线序列：值由采集缓冲区提供，控件订阅 ValuesChanged 重绘</summary>
public sealed class ChartSeriesViewModel
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string ColorHex { get; init; } = "#2E86DE";
    public Func<IReadOnlyList<double>> ValuesProvider { get; init; } = static () => [];

    public event EventHandler? ValuesChanged;
    public void NotifyValuesChanged() => ValuesChanged?.Invoke(this, EventArgs.Empty);
}

/// <summary>设备监控页：采集启停、实时值显示与曲线缓冲区</summary>
public sealed partial class DeviceMonitorViewModel : ViewModelBase
{
    private const int MaxPoints = 600;

    private readonly ICollectionService _collection;
    private readonly Dictionary<string, List<double>> _buffers = new(StringComparer.OrdinalIgnoreCase);

    public override string PageTitle => "设备监控";

    public ObservableCollection<ChartSeriesViewModel> Series { get; }

    public string ToggleText => _collection.IsRunning ? "停止采集" : "开始采集";

    [ObservableProperty] private string _temperatureText = "--";
    [ObservableProperty] private string _pressureText = "--";

    public IAsyncRelayCommand ToggleCommand { get; }

    public DeviceMonitorViewModel(ICollectionService collection, IMessenger messenger)
    {
        _collection = collection;
        _buffers["Temperature"] = [];
        _buffers["Pressure"] = [];

        Series =
        [
            new ChartSeriesViewModel
            {
                Code = "Temperature",
                Name = "Temperature (℃×10)",
                ColorHex = "#2E86DE",
                ValuesProvider = () => _buffers["Temperature"]
            },
            new ChartSeriesViewModel
            {
                Code = "Pressure",
                Name = "Pressure (MPa)",
                ColorHex = "#E67E22",
                ValuesProvider = () => _buffers["Pressure"]
            }
        ];

        ToggleCommand = new AsyncRelayCommand(async () =>
        {
            if (_collection.IsRunning)
            {
                await _collection.StopAsync();
            }
            else await _collection.StartAsync();
            OnPropertyChanged(nameof(ToggleText));
        });

        messenger.Register<ChartDataPushedMessage, string>(this, HmiMessages.Token, (_, m) => Push(m));
    }

    private void Push(ChartDataPushedMessage message)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            dispatcher.Invoke(() => Push(message));
            return;
        }

        if (!_buffers.TryGetValue(message.TagCode, out var buffer))
        {
            return;
        }
        buffer.Add(message.Value);
        while (buffer.Count > MaxPoints) buffer.RemoveAt(0);

        Series.FirstOrDefault(s => s.Code == message.TagCode)?.NotifyValuesChanged();

        if (message.TagCode == "Temperature")
        {
            TemperatureText = $"{message.Value:0.00}";
        }
        if (message.TagCode == "Pressure")
        {
            PressureText = $"{message.Value:0.00}";
        }
    }
}
