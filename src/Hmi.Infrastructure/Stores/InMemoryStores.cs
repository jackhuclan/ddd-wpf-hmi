using Hmi.Domain.Alarms;
using Hmi.Domain.Devices;
using Hmi.Domain.Recipes;
using Hmi.Domain.WorkOrders;

namespace Hmi.Infrastructure.Stores;

/// <summary>设备内存仓储：持有同一对象引用，采集循环直接改写模块状态。</summary>
public sealed class InMemoryDeviceStore : IDeviceStore
{
    private readonly List<Device> _items = [];
    private readonly object _gate = new();

    public IReadOnlyList<Device> GetAll()
    {
        lock (_gate)
        {
            return _items.ToList();
        }
    }

    public Device? GetById(Guid id)
    {
        lock (_gate)
        {
            return _items.FirstOrDefault(d => d.Id == id);
        }
    }

    // 持有同一对象引用，修改即生效
    public void Update(Device device)
    {
    }

    public void ReplaceAll(IEnumerable<Device> devices)
    {
        lock (_gate)
        {
            _items.Clear();
            _items.AddRange(devices);
        }
    }
}

/// <summary>工单内存仓储：按工单编号索引，状态变更直接反映在同一引用上。</summary>
public sealed class InMemoryWorkOrderStore : IWorkOrderStore
{
    private readonly List<WorkOrder> _items = [];
    private readonly object _gate = new();

    public IReadOnlyList<WorkOrder> GetAll()
    {
        lock (_gate)
        {
            return _items.ToList();
        }
    }

    public WorkOrder? GetByCode(string code)
    {
        lock (_gate)
        {
            return _items.FirstOrDefault(o => o.Code == code);
        }
    }

    public void Add(WorkOrder workOrder)
    {
        lock (_gate)
        {
            _items.Add(workOrder);
        }
    }

    // 同一引用，状态流转即生效
    public void Update(WorkOrder workOrder)
    {
    }
}

/// <summary>配方内存仓储：按语义化版本取最新版，并维护唯一启用项。</summary>
public sealed class InMemoryRecipeStore : IRecipeStore
{
    private readonly List<Recipe> _items = [];
    private readonly object _gate = new();

    public IReadOnlyList<Recipe> GetAll()
    {
        lock (_gate)
        {
            return _items.ToList();
        }
    }

    public Recipe? GetByCode(string code)
    {
        lock (_gate)
        {
            return _items.FirstOrDefault(r => r.Code == code);
        }
    }

    public Recipe? GetLatest(string code)
    {
        lock (_gate)
        {
            return _items.Where(r => r.Code == code)
                .OrderByDescending(r => ParseVersion(r.Version))
                .FirstOrDefault();
        }
    }

    public void ReplaceAll(IEnumerable<Recipe> recipes)
    {
        lock (_gate)
        {
            _items.Clear();
            _items.AddRange(recipes);
        }
    }

    public void SetEnabled(string code)
    {
        lock (_gate)
        {
            foreach (var recipe in _items)
            {
                recipe.Status = RecipeStatus.Disabled;
            }

            var target = _items.FirstOrDefault(r => r.Code == code);
            if (target is not null)
            {
                target.Status = RecipeStatus.Enabled;
            }
        }
    }

    private static Version ParseVersion(string version) =>
        Version.TryParse(version, out var parsed) ? parsed : new Version(0, 0);
}

/// <summary>告警内存仓储：活动列表排除已恢复与已清除项。</summary>
public sealed class InMemoryAlarmStore : IAlarmStore
{
    private readonly List<Alarm> _items = [];
    private readonly object _gate = new();

    public void Add(Alarm alarm)
    {
        lock (_gate)
        {
            _items.Add(alarm);
        }
    }

    public IReadOnlyList<Alarm> GetAll()
    {
        lock (_gate)
        {
            return _items.ToList();
        }
    }

    public Alarm? GetById(Guid id)
    {
        lock (_gate)
        {
            return _items.FirstOrDefault(a => a.Id == id);
        }
    }

    public IReadOnlyList<Alarm> GetActive()
    {
        lock (_gate)
        {
            return _items.Where(a => !a.IsCleared && !a.IsRecovered).ToList();
        }
    }
}
