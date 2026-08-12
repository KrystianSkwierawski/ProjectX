using ProjectX.Domain.Enums;

namespace ProjectX.Application.CraftingRecipes.Queries.GetCraftingRecipes;

public class CraftingRecipeDto
{
    public CraftingRecipeEnum Id { get; set; }

    public required CraftingRecipeRequirementDto Requirement { get; set; }

    public required CraftingRecipeRewardDto Reward { get; set; }

    public override string ToString()
    {
        return $"{nameof(CraftingRecipeDto)} {{ Id = {Id} }}";
    }
}
