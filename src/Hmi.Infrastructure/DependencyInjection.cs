using CommunityToolkit.Mvvm.Messaging;
using Hmi.Application.Configuration;
using Hmi.Application.Handlers;
using Hmi.Application.Messages;
using Hmi.Application.Services;
using Hmi.Domain.Alarms;
using Hmi.Domain.Devices;
using Hmi.Domain.Recipes;
using Hmi.Domain.WorkOrders;
using Hmi.Infrastructure.Communications;
using Hmi.Infrastructure.Configuration;
using Hmi.Infrastructure.Messaging;
using Hmi.Infrastructure.Stores;
using Hmi.Infrastructure.Sync;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hmi.Infrastructure;

/// <summary>基础设施层 DI 注册入口：仓储、数据源、消息总线、事件处理器与后台服务</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddHmiInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IConfigService, ConfigService>();
        services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
        services.AddSingleton<IUiEventPublisher, UiEventPublisher>();

        services.AddSingleton<IDeviceStore, InMemoryDeviceStore>();
        services.AddSingleton<IWorkOrderStore, InMemoryWorkOrderStore>();
        services.AddSingleton<IRecipeStore, InMemoryRecipeStore>();
        services.AddSingleton<IAlarmStore, InMemoryAlarmStore>();

        services.AddSingleton<IDataSource, MemorySimDataSource>();
        services.AddSingleton<ICollectionService, CollectionService>();
        services.AddSingleton<IOutboxSyncService, OutboxSyncService>();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateWorkOrderHandler).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
        });

        return services;
    }
}
