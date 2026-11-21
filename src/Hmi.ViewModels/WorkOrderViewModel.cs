using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Hmi.Application.Commands;
using Hmi.Application.Messages;
using Hmi.Domain.Devices;
using Hmi.Domain.WorkOrders;
using MediatR;

namespace Hmi.ViewModels;

/// <summary>工单列表行视图模型</summary>
public sealed class WorkOrderRow
{
    public WorkOrder Order { get; }
    public WorkOrderRow(WorkOrder order) => Order = order;
    public string Code => Order.Code;
    public string Status => Order.StatusText;
    public string RecipeCode => Order.RecipeCode ?? string.Empty;
    public string DeviceText => Order.DeviceId?.ToString() ?? string.Empty;
}

/// <summary>工单管理页：创建、指派、执行、归档与配置刷新</summary>
public sealed partial class WorkOrderViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly IWorkOrderStore _orders;
    private readonly IDeviceStore _devices;
    private readonly IMessenger _messenger;

    public override string PageTitle => "工单管理";

    public ObservableCollection<WorkOrderRow> Orders { get; } = [];
    public ObservableCollection<Device> Devices { get; } = [];

    [ObservableProperty] private string _newOrderCode = $"WO-{DateTime.Now:yyyyMMddHHmmss}";
    [ObservableProperty] private Device? _selectedDevice;
    [ObservableProperty] private string _recipeCodeInput = string.Empty;
    [ObservableProperty] private WorkOrderRow? _selectedOrder;

    public string FooterText => $"共 {Orders.Count} 张工单，{Devices.Count} 台配置设备";

    public IAsyncRelayCommand CreateCommand { get; }
    public IAsyncRelayCommand AssignCommand { get; }
    public IAsyncRelayCommand StartCommand { get; }
    public IAsyncRelayCommand CompleteCommand { get; }
    public IAsyncRelayCommand RefreshCommand { get; }

    public WorkOrderViewModel(
        IMediator mediator,
        IWorkOrderStore orders,
        IDeviceStore devices,
        IMessenger messenger)
    {
        _mediator = mediator;
        _orders = orders;
        _devices = devices;
        _messenger = messenger;

        CreateCommand = new AsyncRelayCommand(CreateAsync, () => !string.IsNullOrWhiteSpace(NewOrderCode));
        AssignCommand = new AsyncRelayCommand(AssignAsync, () => SelectedOrder is not null && SelectedDevice is not null);
        StartCommand = new AsyncRelayCommand(StartAsync,
            () => SelectedOrder?.Order.Status == WorkOrderStatus.Assigned);
        CompleteCommand = new AsyncRelayCommand(CompleteAsync,
            () => SelectedOrder?.Order.Status == WorkOrderStatus.Running);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);

        messenger.Register<WorkOrderListChangedMessage, string>(this, HmiMessages.Token, (_, _) => Refresh());
        Refresh();
    }

    partial void OnNewOrderCodeChanged(string value) => CreateCommand.NotifyCanExecuteChanged();

    partial void OnSelectedOrderChanged(WorkOrderRow? value)
    {
        AssignCommand.NotifyCanExecuteChanged();
        StartCommand.NotifyCanExecuteChanged();
        CompleteCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedDeviceChanged(Device? value) => AssignCommand.NotifyCanExecuteChanged();

    private async Task CreateAsync()
    {
        var tip = await _mediator.Send(new CreateWorkOrderCommand(NewOrderCode));
        NewOrderCode = $"WO-{DateTime.Now:yyyyMMddHHmmss}";
        _messenger.Send(new StatusMessage(tip), HmiMessages.Token);
        Refresh();
    }

    private async Task AssignAsync()
    {
        if (SelectedOrder is null || SelectedDevice is null)
        {
            return;
        }
        var tip = await _mediator.Send(new AssignWorkOrderCommand(
            SelectedOrder.Code, SelectedDevice.Id, RecipeCodeInput));
        _messenger.Send(new StatusMessage(tip), HmiMessages.Token);
        Refresh();
    }

    private async Task StartAsync()
    {
        if (SelectedOrder is null)
        {
            return;
        }
        var tip = await _mediator.Send(new StartWorkOrderCommand(SelectedOrder.Code));
        _messenger.Send(new StatusMessage(tip), HmiMessages.Token);
        Refresh();
    }

    private async Task CompleteAsync()
    {
        if (SelectedOrder is null)
        {
            return;
        }
        var tip = await _mediator.Send(new CompleteWorkOrderCommand(SelectedOrder.Code));
        _messenger.Send(new StatusMessage(tip), HmiMessages.Token);
        Refresh();
    }

    private async Task RefreshAsync()
    {
        var tip = await _mediator.Send(new ReloadConfigCommand());
        _messenger.Send(new StatusMessage(tip), HmiMessages.Token);
        Refresh();
    }

    private void Refresh()
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            dispatcher.Invoke(Refresh);
            return;
        }

        var selectedCode = SelectedOrder?.Code;
        Orders.Clear();
        foreach (var order in _orders.GetAll().OrderByDescending(o => o.CreatedAt))
            Orders.Add(new WorkOrderRow(order));

        Devices.Clear();
        foreach (var device in _devices.GetAll()) Devices.Add(device);

        SelectedOrder = Orders.FirstOrDefault(o => o.Code == selectedCode);
        OnPropertyChanged(nameof(FooterText));
        AssignCommand.NotifyCanExecuteChanged();
        StartCommand.NotifyCanExecuteChanged();
        CompleteCommand.NotifyCanExecuteChanged();
    }
}
