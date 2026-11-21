using Hmi.Application.Configuration;
using Microsoft.Extensions.Configuration;

namespace Hmi.Infrastructure.Configuration;

/// <summary>
/// 配置热加载：每次 Reload 重新绑定根配置（appsettings.json）。
/// 语义等价于 IOptionsMonitor 的「改完刷新即生效」。
/// </summary>
public sealed class ConfigService(IConfiguration configuration) : IConfigService
{
    public AppOptions Current { get; private set; } = new();

    public void Reload()
    {
        Current = configuration.Get<AppOptions>() ?? new AppOptions();
    }
}
