using ProjectX.Application.CharacterInventories.Queries.GetCharacterInventory;

namespace ProjectX.Application.CraftingRecipes.Queries.GetCraftingRecipes;
public class CraftingRecipeRequirementDto
{
    // FIXME: array?
    public IList<InventoryItem> Items { get; set; } = [];

    public int Level { get; set; }
}
