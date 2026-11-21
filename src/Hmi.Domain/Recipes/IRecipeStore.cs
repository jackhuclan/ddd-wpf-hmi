namespace Hmi.Domain.Recipes;

/// <summary>配方仓储契约：按语义化版本取最新版本并维护唯一启用项</summary>
public interface IRecipeStore
{
    IReadOnlyList<Recipe> GetAll();
    Recipe? GetByCode(string code);
    /// <summary>取某编号的最新版本（按语义化版本比较）</summary>
    Recipe? GetLatest(string code);
    void ReplaceAll(IEnumerable<Recipe> recipes);
    void SetEnabled(string code);
}
