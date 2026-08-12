using ProjectX.Domain.Common;
using ProjectX.Domain.Crafting;
using ProjectX.Domain.Enums;

namespace ProjectX.Domain.Entities;

public class CraftingRecipe : BaseAuditableEntity
{
    public CraftingRecipeEnum Id { get; set; }

    public CraftingRecipeTypeEnum Type { get; set; }

    public required string Name { get; set; }

    public required CraftingRecipeRequirement Requirement { get; set; }

    public required CraftingRecipeReward Reward { get; set; }

    public StatusEnum Status { get; set; }
}
