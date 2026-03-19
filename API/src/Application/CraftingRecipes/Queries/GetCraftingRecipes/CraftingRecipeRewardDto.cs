using ProjectX.Application.CharacterInventories.Queries.GetCharacterInventory;

namespace ProjectX.Application.CraftingRecipes.Queries.GetCraftingRecipes;
public class CraftingRecipeRewardDto
{
    public required InventoryItemDto Item { get; set; }

    public int Experience { get; set; }
}
