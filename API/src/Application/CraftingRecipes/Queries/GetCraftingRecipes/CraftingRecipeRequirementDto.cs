using ProjectX.Application.CharacterInventories.Queries.GetCharacterInventory;

namespace ProjectX.Application.CraftingRecipes.Queries.GetCraftingRecipes;
public class CraftingRecipeRequirementDto
{
    public InventoryItem[] Items { get; set; } = [];

    public int Level { get; set; }
}
