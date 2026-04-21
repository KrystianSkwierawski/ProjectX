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

    [CraftingRecipeParameters(
        Type = CraftingRecipeTypeEnum.Blacksmithing,
        Requirement = """{ "Items": [ { "Type": 300, "Count": 1 } ], "Level": 1 }""",
        Reward = """{ "Item": { "Type": 304, "Count": 1 }, "Experience": 1000 }"""
    )]
    PurpleBar,

    [CraftingRecipeParameters(
        Type = CraftingRecipeTypeEnum.Blacksmithing,
        Requirement = """{ "Items": [ { "Type": 301, "Count": 1 } ], "Level": 1 }""",
        Reward = """{ "Item": { "Type": 305, "Count": 1 }, "Experience": 1000 }"""
    )]
    WhiteBar,

    [CraftingRecipeParameters(
        Type = CraftingRecipeTypeEnum.Blacksmithing,
        Requirement = """{ "Items": [ { "Type": 302, "Count": 1 } ], "Level": 1 }""",
        Reward = """{ "Item": { "Type": 306, "Count": 1 }, "Experience": 1000 }"""
    )]
    CopperBar,

    [CraftingRecipeParameters(
        Type = CraftingRecipeTypeEnum.Blacksmithing,
        Requirement = """{ "Items": [ { "Type": 303, "Count": 1 } ], "Level": 1 }""",
        Reward = """{ "Item": { "Type": 307, "Count": 1 }, "Experience": 1000 }"""
    )]
    BlackBar,

    [CraftingRecipeParameters(
        Type = CraftingRecipeTypeEnum.Alchemy,
        Requirement = """{ "Items": [ { "Type": 500, "Count": 1 } ], "Level": 1 }""",
        Reward = """{ "Item": { "Type": 501, "Count": 1 }, "Experience": 1000 }"""
    )]
    HealthPotion
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

