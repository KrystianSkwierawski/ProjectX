using ProjectX.Domain.Enums;

namespace ProjectX.Domain.Entities;

public class CraftingRecipe
{
    public CraftingRecipieEnum Id { get; set; }

    public CraftingRecipieTypeEnum Type { get; set; }

    public string Name { get; set; }

    public required string Requirement { get; set; }

    public required string Reward { get; set; }

    public StatusEnum Status { get; set; }

    public DateTime ModDate { get; set; }
}
