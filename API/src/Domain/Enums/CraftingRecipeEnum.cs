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
        Requirement = """{ "Items": [ { "Type": 301, "Count": 1 } ], "Level": 2 }""",
        Reward = """{ "Item": { "Type": 305, "Count": 1 }, "Experience": 1000 }"""
    )]
    WhiteBar,

    [CraftingRecipeParameters(
        Type = CraftingRecipeTypeEnum.Blacksmithing,
        Requirement = """{ "Items": [ { "Type": 302, "Count": 1 } ], "Level": 3 }""",
        Reward = """{ "Item": { "Type": 306, "Count": 1 }, "Experience": 1000 }"""
    )]
    CopperBar,

    [CraftingRecipeParameters(
        Type = CraftingRecipeTypeEnum.Blacksmithing,
        Requirement = """{ "Items": [ { "Type": 303, "Count": 1 } ], "Level": 4 }""",
        Reward = """{ "Item": { "Type": 307, "Count": 1 }, "Experience": 1000 }"""
    )]
    BlackBar,

    [CraftingRecipeParameters(
        Type = CraftingRecipeTypeEnum.Alchemy,
        Requirement = """{ "Items": [ { "Type": 500, "Count": 1 } ], "Level": 1 }""",
        Reward = """{ "Item": { "Type": 501, "Count": 1 }, "Experience": 1000 }"""
    )]
    HealthPotion,

    [CraftingRecipeParameters(
        Type = CraftingRecipeTypeEnum.Blacksmithing,
        Requirement = """{ "Items": [ { "Type": 400, "Count": 1 }, { "Type": 304, "Count": 1 } ], "Level": 1 }""",
        Reward = """{ "Item": { "Type": 1009, "Count": 1 }, "Experience": 1000 }"""
    )]
    AmmoArrow1,

    [CraftingRecipeParameters(
        Type = CraftingRecipeTypeEnum.Blacksmithing,
        Requirement = """{ "Items": [ { "Type": 400, "Count": 2 }, { "Type": 305, "Count": 2 } ], "Level": 2 }""",
        Reward = """{ "Item": { "Type": 1010, "Count": 1 }, "Experience": 1000 }"""
    )]
    AmmoArrow2,

    [CraftingRecipeParameters(
        Type = CraftingRecipeTypeEnum.Blacksmithing,
        Requirement = """{ "Items": [ { "Type": 400, "Count": 3 }, { "Type": 306, "Count": 3 } ], "Level": 3 }""",
        Reward = """{ "Item": { "Type": 1011, "Count": 1 }, "Experience": 1000 }"""
    )]
    AmmoArrow3,

    [CraftingRecipeParameters(
        Type = CraftingRecipeTypeEnum.Blacksmithing,
        Requirement = """{ "Items": [ { "Type": 300, "Count": 1 }, { "Type": 304, "Count": 1 } ], "Level": 1 }""",
        Reward = """{ "Item": { "Type": 1012, "Count": 1 }, "Experience": 1000 }"""
    )]
    AmmoRune1,

    [CraftingRecipeParameters(
        Type = CraftingRecipeTypeEnum.Blacksmithing,
        Requirement = """{ "Items": [ { "Type": 301, "Count": 2 }, { "Type": 305, "Count": 2 } ], "Level": 2 }""",
        Reward = """{ "Item": { "Type": 1013, "Count": 1 }, "Experience": 1000 }"""
    )]
    AmmoRune2,

    [CraftingRecipeParameters(
        Type = CraftingRecipeTypeEnum.Blacksmithing,
        Requirement = """{ "Items": [ { "Type": 302, "Count": 3 }, { "Type": 306, "Count": 3 } ], "Level": 3 }""",
        Reward = """{ "Item": { "Type": 1014, "Count": 1 }, "Experience": 1000 }"""
    )]
    AmmoRune3,

    [CraftingRecipeParameters(
        Type = CraftingRecipeTypeEnum.Blacksmithing,
        Requirement = """{ "Items": [ { "Type": 304, "Count": 1 } ], "Level": 1 }""",
        Reward = """{ "Item": { "Type": 1015, "Count": 1 }, "Experience": 1000 }"""
    )]
    AmmoFeather1,

    [CraftingRecipeParameters(
        Type = CraftingRecipeTypeEnum.Blacksmithing,
        Requirement = """{ "Items": [ { "Type": 305, "Count": 2 } ], "Level": 2 }""",
        Reward = """{ "Item": { "Type": 1016, "Count": 1 }, "Experience": 1000 }"""
    )]
    AmmoFeather2,

    [CraftingRecipeParameters(
        Type = CraftingRecipeTypeEnum.Blacksmithing,
        Requirement = """{ "Items": [ { "Type": 306, "Count": 3 } ], "Level": 3 }""",
        Reward = """{ "Item": { "Type": 1017, "Count": 1 }, "Experience": 1000 }"""
    )]
    AmmoFeather3,

    [CraftingRecipeParameters(
        Type = CraftingRecipeTypeEnum.Alchemy,
        Requirement = """{ "Items": [ { "Type": 500, "Count": 1 } ], "Level": 1 }""",
        Reward = """{ "Item": { "Type": 1018, "Count": 1 }, "Experience": 1000 }"""
    )]
    AmmoOil1,

    [CraftingRecipeParameters(
        Type = CraftingRecipeTypeEnum.Alchemy,
        Requirement = """{ "Items": [ { "Type": 500, "Count": 2 } ], "Level": 2 }""",
        Reward = """{ "Item": { "Type": 1019, "Count": 1 }, "Experience": 1000 }"""
    )]
    AmmoOil2,

    [CraftingRecipeParameters(
        Type = CraftingRecipeTypeEnum.Alchemy,
        Requirement = """{ "Items": [ { "Type": 500, "Count": 3 } ], "Level": 3 }""",
        Reward = """{ "Item": { "Type": 1020, "Count": 1 }, "Experience": 1000 }"""
    )]
    AmmoOil3
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

