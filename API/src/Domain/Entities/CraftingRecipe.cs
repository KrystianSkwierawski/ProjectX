using ProjectX.Domain.Enums;

namespace ProjectX.Domain.Entities;

public class CraftingRecipe
{
    public CraftingRecipeEnum Id { get; set; }

    public CraftingRecipeTypeEnum Type { get; set; }

    public string Name { get; set; }

    public string Requirement { get; set; }

    public string Reward { get; set; }

    public StatusEnum Status { get; set; }

    public DateTime ModDate { get; set; }
}
