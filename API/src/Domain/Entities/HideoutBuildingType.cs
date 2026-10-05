using ProjectX.Domain.Common;
using ProjectX.Domain.Crafting;
using ProjectX.Domain.Enums;

namespace ProjectX.Domain.Entities;

public class HideoutBuildingType : BaseAuditableEntity
{
    public HideoutBuildingEnum Id { get; set; }

    public required string Name { get; set; }

    public required CraftingRecipeRequirement Requirement { get; set; }

    public int BuildTime { get; set; }

    public StatusEnum Status { get; set; }
}
