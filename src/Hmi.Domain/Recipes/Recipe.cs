namespace Hmi.Domain.Recipes;

/// <summary>配方：编号 + 版本 + 点位清单，来自配置</summary>
public sealed class Recipe
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public RecipeStatus Status { get; set; } = RecipeStatus.Disabled;
    public List<RecipePoint> Points { get; set; } = [];

    public string StatusText => Status == RecipeStatus.Enabled ? "已启用" : "未启用";
}

/// <summary>配位点位目标值</summary>
public sealed class RecipePoint
{
    public string Code { get; set; } = string.Empty;
    public double TargetValue { get; set; }
}

/// <summary>配方启用状态</summary>
public enum RecipeStatus
{
    Disabled,
    Enabled
}
