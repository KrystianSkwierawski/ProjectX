using System.Reflection;
using ProjectX.Domain.Attributes;

namespace ProjectX.Domain.Enums;

public enum CraftingRecipeEnum : short
{
    None,

    [CraftingRecipeParameters(
        Type = CraftingRecipeTypeEnum.Cooking,
        Requirement = "{ Items: [ { Type: 2, Count: 1 } ], Level: 1 }",
        Reward = "{ Item: { Type: 3, Count: 1 }, Experience: 1000 }"
    )]
    CookedFish,
}

public static class CraftingRecipeEnumExtensions
{
    public static CraftingRecipeParametersAttribute GetParameters(this CraftingRecipeEnum value)
    {
        var member = value
            .GetType()
            .GetMember(value.ToString())
            .First();

        return member.GetCustomAttribute<CraftingRecipeParametersAttribute>() ?? throw new ArgumentNullException(nameof(value));
    }
}

