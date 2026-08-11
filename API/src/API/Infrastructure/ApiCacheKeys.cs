using ProjectX.Domain.Enums;

namespace ProjectX.API.Infrastructure;

public static class ApiCacheKeys
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(1);

    public static string Quest(QuestEnum questId, LanguageEnum language) => $"Quest_{questId}_{language}";

    public static string Quests(LanguageEnum language) => $"Quests_{language}";

    public static string CraftingRecipes(CraftingRecipeTypeEnum type) => $"CraftingRecipes_{type}";
}
