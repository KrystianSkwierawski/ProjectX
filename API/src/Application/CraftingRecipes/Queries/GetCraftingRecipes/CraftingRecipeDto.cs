namespace ProjectX.Application.CraftingRecipes.Queries.GetCraftingRecipes;
public class CraftingRecipeDto
{
    public required CraftingRecipeRequirementDto Requirement { get; set; }

    public required CraftingRecipeRewardDto Reward { get; set; }
}
