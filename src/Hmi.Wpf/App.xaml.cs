using System.Windows;
using Hmi.Application.Configuration;
using Hmi.Application.Services;
using Hmi.Domain.Devices;
using Hmi.Domain.Recipes;
using Hmi.Infrastructure;
using Hmi.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Hmi.Wpf;

/// <summary>Prism 式应用外壳的装配点：DI + 配置 + 模块注册</summary>
// 使用全限定名避免与 Hmi.Application 命名空间冲突
public partial class App : System.Windows.Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File("logs/hmi-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
            .CreateLogger();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddHmiInfrastructure(configuration);

        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<ShellViewModel>();
        services.AddSingleton<DeviceMonitorViewModel>();
        services.AddSingleton<AlarmCenterViewModel>();
        services.AddSingleton<WorkOrderViewModel>();
        services.AddSingleton<RecipeViewModel>();
        services.AddTransient<MainWindow>();

        Services = services.BuildServiceProvider();

        // 四个模块对应四个限界上下文，按需懒加载
        var navigation = Services.GetRequiredService<INavigationService>();
        navigation.Register("device", () => Services.GetRequiredService<DeviceMonitorViewModel>());
        navigation.Register("alarm", () => Services.GetRequiredService<AlarmCenterViewModel>());
        navigation.Register("workorder", () => Services.GetRequiredService<WorkOrderViewModel>());
        navigation.Register("recipe", () => Services.GetRequiredService<RecipeViewModel>());

        // 首次把配置灌入仓储；之后由 ReloadConfigCommand 热加载
        var options = Services.GetRequiredService<IConfigService>().Current;
        Services.GetRequiredService<IDeviceStore>().ReplaceAll(options.Devices);
        Services.GetRequiredService<IRecipeStore>().ReplaceAll(options.Recipes);

        Services.GetRequiredService<MainWindow>().Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Services.GetRequiredService<ICollectionService>().StopAsync().GetAwaiter().GetResult();
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
