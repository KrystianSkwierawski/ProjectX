using ProjectX.Application.CharacterInventories.Queries.GetCharacterInventory;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.CraftingRecipes.Queries.GetCraftingRecipes;
public class CraftingRecipeDto
{
    public CraftingRecipieTypeEnum Type { get; set; }

    public required CraftingRecipeRequirementDto Requirement { get; set; }

    public required CraftingRecipeRewardDto Reward { get; set; }
}
