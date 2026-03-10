using System.Reflection;
using ProjectX.Domain.Attributes;

namespace ProjectX.Domain.Enums;

public enum CraftingRecipieEnum : short
{
    None,

    [CraftingRecipeParameters(
        Type = CraftingRecipieTypeEnum.Cooking,
        Requirement = "{ Items: [{Type: 2, Count: 1}], Level: 1 }",
        Reward = "{ Item: { Type: 2, Count: 1 }, Experience: 1000 }"
    )]
    CookedFish,
}

public static class CraftingRecipieEnumExtensions
{
    public static CraftingRecipeParametersAttribute GetParameters(this CraftingRecipieEnum value)
    {
        var member = value
            .GetType()
            .GetMember(value.ToString())
            .First();

        return member.GetCustomAttribute<CraftingRecipeParametersAttribute>() ?? throw new ArgumentNullException(nameof(value));
    }
}

