using ProjectX.Domain.Enums;

namespace ProjectX.Domain.Crafting;

public sealed record CraftingRecipeItem(InventoryItemEnum Type, int Count);

public sealed record CraftingRecipeRequirement(CraftingRecipeItem[] Items, int Level);

public sealed record CraftingRecipeReward(CraftingRecipeItem Item, int Experience);

public sealed record CraftingRecipeDefinition(
    CraftingRecipeTypeEnum Type,
    CraftingRecipeRequirement Requirement,
    CraftingRecipeReward Reward,
    StatusEnum Status = StatusEnum.Active);
