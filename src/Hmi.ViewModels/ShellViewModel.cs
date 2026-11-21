using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Hmi.Application.Messages;
using Hmi.Application.Services;

namespace Hmi.ViewModels;

/// <summary>UI 消息统一 token，与基础设施层 UiEventPublisher.Token 一致</summary>
public static class HmiMessages
{
    public const string Token = "hmi-ui";
}

/// <summary>主窗口视图模型：导航、采集开关、连接状态与底部状态栏</summary>
public sealed partial class ShellViewModel : ViewModelBase
{
    private readonly INavigationService _navigation;
    private readonly ICollectionService _collection;
    private readonly IMessenger _messenger;
    private readonly DispatcherTimer _clock;

    public ObservableCollection<NavigationItem> NavigationItems { get; } =
    [
        new() { Key = "device", Title = "设备监控", Glyph = "▣" },
        new() { Key = "alarm", Title = "告警中心", Glyph = "▸" },
        new() { Key = "workorder", Title = "工单管理", Glyph = "☰" },
        new() { Key = "recipe", Title = "配方管理", Glyph = "⚙" }
    ];

    public ViewModelBase? CurrentViewModel => _navigation.Current;

    public string ArchitectureText =>
        "Prism 9 模块化 + DDD 四限界上下文    ·    .NET 10    ·    " + DateTime.Now.ToString("yyyy-MM-dd");

    [ObservableProperty] private string _currentPageTitle = "设备监控";
    [ObservableProperty] private string _clockText = DateTime.Now.ToString("HH:mm:ss");
    [ObservableProperty] private string _connectionText = "设备未连接";
    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private string _statusTip = "就绪";

    public IRelayCommand NavigateCommand { get; }

    public ShellViewModel(
        INavigationService navigation,
        ICollectionService collection,
        IMessenger messenger)
    {
        _navigation = navigation;
        _collection = collection;
        _messenger = messenger;

        NavigateCommand = new RelayCommand<string>(key =>
        {
            if (key is not null && _navigation.NavigateTo(key))
            {
                OnNavigated();
            }
        });

        _navigation.Navigated += (_, _) => OnNavigated();

        _messenger.Register<ConnectionStateChangedMessage, string>(this, HmiMessages.Token, (_, m) =>
        {
            IsConnected = m.IsConnected;
            ConnectionText = m.Text;
        });
        _messenger.Register<StatusMessage, string>(this, HmiMessages.Token, (_, m) => StatusTip = m.Text);

        _clock = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clock.Tick += (_, _) => ClockText = DateTime.Now.ToString("HH:mm:ss");
        _clock.Start();

        _navigation.NavigateTo("device");
        OnNavigated();
    }

    private void OnNavigated()
    {
        OnPropertyChanged(nameof(CurrentViewModel));
        var item = NavigationItems.FirstOrDefault(i => i.Key == _navigation.CurrentKey);
        foreach (var nav in NavigationItems) nav.IsSelected = ReferenceEquals(nav, item);
        CurrentPageTitle = item?.Title ?? string.Empty;
        StatusTip = $"当前页：{CurrentPageTitle}";
    }
}
