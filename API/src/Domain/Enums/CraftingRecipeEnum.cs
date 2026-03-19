using System.Reflection;
using ProjectX.Domain.Attributes;

namespace ProjectX.Domain.Enums;

public enum CraftingRecipeEnum : short
{
    None,

    [CraftingRecipeParameters(
        Type = CraftingRecipeTypeEnum.Cooking,
        Requirement = """{ "Items": [ { "Type": 200, "Count": 1 } ], "Level": 1 }""",
        Reward = """{ "Item": { "Type": 201, "Count": 1 }, "Experience": 1000 }"""
    )]
    CookedFish,

    [CraftingRecipeParameters(
        Type = CraftingRecipeTypeEnum.Cooking,
        Requirement = """{ "Items": [ { "Type": 201, "Count": 1 }, { "Type": 202, "Count": 1 } ], "Level": 1 }""",
        Reward = """{ "Item": { "Type": 203, "Count": 1 }, "Experience": 1000 }"""
    )]
    Sushi,
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

