using CommunityToolkit.Mvvm.ComponentModel;

namespace Hmi.ViewModels;

/// <summary>所有页面视图模型的基类，提供页标题契约</summary>
public abstract class ViewModelBase : ObservableObject
{
    public virtual string PageTitle => string.Empty;
}

/// <summary>左侧导航项</summary>
public sealed class NavigationItem : ObservableObject
{
    public string Key { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Glyph { get; init; } = string.Empty;

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}

/// <summary>单窗口外壳导航服务，页面按需装配（懒加载）</summary>
public interface INavigationService
{
    ViewModelBase? Current { get; }
    string CurrentKey { get; }
    event EventHandler? Navigated;
    void Register(string key, Func<ViewModelBase> factory);
    bool NavigateTo(string key);
}

/// <summary>单窗口导航服务：按 key 懒加载并缓存页面视图模型</summary>
public sealed class NavigationService : INavigationService
{
    private readonly Dictionary<string, Func<ViewModelBase>> _factories = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ViewModelBase> _instances = new(StringComparer.OrdinalIgnoreCase);

    public ViewModelBase? Current { get; private set; }
    public string CurrentKey { get; private set; } = string.Empty;

    public event EventHandler? Navigated;

    public void Register(string key, Func<ViewModelBase> factory) => _factories[key] = factory;

    public bool NavigateTo(string key)
    {
        if (!_factories.TryGetValue(key, out var factory))
        {
            return false;
        }
        if (!_instances.TryGetValue(key, out var vm))
        {
            vm = factory();
            _instances[key] = vm;
        }
        Current = vm;
        CurrentKey = key;
        Navigated?.Invoke(this, EventArgs.Empty);
        return true;
    }
}
