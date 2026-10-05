using ProjectX.Application.CharacterInventories.Queries.GetCharacterInventory;
using ProjectX.Domain.Crafting;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.Hideouts;

public class HideoutDto
{
    public string Outcome { get; set; } = "Applied";

    public DateTimeOffset CurrentTime { get; set; }

    public HideoutBuildingDto[] Buildings { get; set; } = [];

    public CharacterInventoryDto? CharacterInventory { get; set; }
}

public class HideoutBuildingDto
{
    public HideoutBuildingEnum Id { get; set; }

    public required string Name { get; set; }

    public required CraftingRecipeRequirement Requirement { get; set; }

    public int BuildTime { get; set; }

    public DateTimeOffset? BuildStartedAt { get; set; }

    public DateTimeOffset? BuildEndsAt { get; set; }
}
