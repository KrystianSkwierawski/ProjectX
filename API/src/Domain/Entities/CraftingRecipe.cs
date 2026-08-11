using ProjectX.Domain.Enums;

using ProjectX.Domain.Common;

namespace ProjectX.Domain.Entities;

public class CraftingRecipe : BaseAuditableEntity
{
    public CraftingRecipeEnum Id { get; set; }

    public CraftingRecipeTypeEnum Type { get; set; }

    public required string Name { get; set; }

    public required string Requirement { get; set; }

    public required string Reward { get; set; }

    public StatusEnum Status { get; set; }

}
