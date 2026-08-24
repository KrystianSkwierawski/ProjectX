using ProjectX.Domain.Crafting;

namespace ProjectX.Domain.Enums;

public enum CraftingRecipeEnum : short
{
    None,
    CookedFish,
    Sushi,
    PurpleBar,
    WhiteBar,
    CopperBar,
    BlackBar,
    HealthPotion,
    AmmoArrow1,
    AmmoArrow2,
    AmmoArrow3,
    AmmoRune1,
    AmmoRune2,
    AmmoRune3,
    AmmoFeather1,
    AmmoFeather2,
    AmmoFeather3,
    AmmoOil1,
    AmmoOil2,
    AmmoOil3,
    StrengthPotion,
    SpeedPotion
}

public static class CraftingRecipeEnumExtensions
{
    private const int DefaultExperience = 1000;

    public static CraftingRecipeDefinition GetDefinition(this CraftingRecipeEnum value)
    {
        return value switch
        {
            CraftingRecipeEnum.CookedFish => Create(CraftingRecipeTypeEnum.Cooking, 1, InventoryItemEnum.CookedFish, (InventoryItemEnum.Fish, 1)),
            CraftingRecipeEnum.Sushi => Create(CraftingRecipeTypeEnum.Cooking, 1, InventoryItemEnum.Sushi, (InventoryItemEnum.CookedFish, 1), (InventoryItemEnum.Rice, 1)),
            CraftingRecipeEnum.PurpleBar => Create(CraftingRecipeTypeEnum.Blacksmithing, 1, InventoryItemEnum.PurpleBar, (InventoryItemEnum.PurpleOre, 1)),
            CraftingRecipeEnum.WhiteBar => Create(CraftingRecipeTypeEnum.Blacksmithing, 2, InventoryItemEnum.WhiteBar, (InventoryItemEnum.WhiteOre, 1)),
            CraftingRecipeEnum.CopperBar => Create(CraftingRecipeTypeEnum.Blacksmithing, 3, InventoryItemEnum.CopperBar, (InventoryItemEnum.CopperOre, 1)),
            CraftingRecipeEnum.BlackBar => Create(CraftingRecipeTypeEnum.Blacksmithing, 4, InventoryItemEnum.BlackBar, (InventoryItemEnum.BlackOre, 1)),
            CraftingRecipeEnum.HealthPotion => Create(CraftingRecipeTypeEnum.Alchemy, 1, InventoryItemEnum.HealthPotion, (InventoryItemEnum.Chamomile, 1)),
            CraftingRecipeEnum.AmmoArrow1 => Create(CraftingRecipeTypeEnum.Blacksmithing, 1, InventoryItemEnum.AmmoArrow1, (InventoryItemEnum.Wood, 1), (InventoryItemEnum.PurpleBar, 1)),
            CraftingRecipeEnum.AmmoArrow2 => Create(CraftingRecipeTypeEnum.Blacksmithing, 2, InventoryItemEnum.AmmoArrow2, (InventoryItemEnum.Wood, 2), (InventoryItemEnum.WhiteBar, 2)),
            CraftingRecipeEnum.AmmoArrow3 => Create(CraftingRecipeTypeEnum.Blacksmithing, 3, InventoryItemEnum.AmmoArrow3, (InventoryItemEnum.Wood, 3), (InventoryItemEnum.CopperBar, 3)),
            CraftingRecipeEnum.AmmoRune1 => Create(CraftingRecipeTypeEnum.Blacksmithing, 1, InventoryItemEnum.AmmoRune1, (InventoryItemEnum.PurpleOre, 1), (InventoryItemEnum.PurpleBar, 1)),
            CraftingRecipeEnum.AmmoRune2 => Create(CraftingRecipeTypeEnum.Blacksmithing, 2, InventoryItemEnum.AmmoRune2, (InventoryItemEnum.WhiteOre, 2), (InventoryItemEnum.WhiteBar, 2)),
            CraftingRecipeEnum.AmmoRune3 => Create(CraftingRecipeTypeEnum.Blacksmithing, 3, InventoryItemEnum.AmmoRune3, (InventoryItemEnum.CopperOre, 3), (InventoryItemEnum.CopperBar, 3)),
            CraftingRecipeEnum.AmmoFeather1 => Create(CraftingRecipeTypeEnum.Blacksmithing, 1, InventoryItemEnum.AmmoFeather1, (InventoryItemEnum.PurpleBar, 1)),
            CraftingRecipeEnum.AmmoFeather2 => Create(CraftingRecipeTypeEnum.Blacksmithing, 2, InventoryItemEnum.AmmoFeather2, (InventoryItemEnum.WhiteBar, 2)),
            CraftingRecipeEnum.AmmoFeather3 => Create(CraftingRecipeTypeEnum.Blacksmithing, 3, InventoryItemEnum.AmmoFeather3, (InventoryItemEnum.CopperBar, 3)),
            CraftingRecipeEnum.AmmoOil1 => Create(CraftingRecipeTypeEnum.Alchemy, 1, InventoryItemEnum.AmmoOil1, (InventoryItemEnum.Chamomile, 1)),
            CraftingRecipeEnum.AmmoOil2 => Create(CraftingRecipeTypeEnum.Alchemy, 2, InventoryItemEnum.AmmoOil2, (InventoryItemEnum.Chamomile, 2)),
            CraftingRecipeEnum.AmmoOil3 => Create(CraftingRecipeTypeEnum.Alchemy, 3, InventoryItemEnum.AmmoOil3, (InventoryItemEnum.Chamomile, 3)),
            CraftingRecipeEnum.StrengthPotion => Create(CraftingRecipeTypeEnum.Alchemy, 1, InventoryItemEnum.StrengthPotion, (InventoryItemEnum.Chamomile, 2)),
            CraftingRecipeEnum.SpeedPotion => Create(CraftingRecipeTypeEnum.Alchemy, 1, InventoryItemEnum.SpeedPotion, (InventoryItemEnum.Chamomile, 2)),
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown crafting recipe.")
        };
    }

    private static CraftingRecipeDefinition Create(
        CraftingRecipeTypeEnum type,
        int level,
        InventoryItemEnum reward,
        params (InventoryItemEnum Type, int Count)[] requirements)
    {
        return new CraftingRecipeDefinition(
            type,
            new CraftingRecipeRequirement(
                requirements.Select(item => new CraftingRecipeItem(item.Type, item.Count)).ToArray(),
                level),
            new CraftingRecipeReward(new CraftingRecipeItem(reward, 1), DefaultExperience));
    }
}
