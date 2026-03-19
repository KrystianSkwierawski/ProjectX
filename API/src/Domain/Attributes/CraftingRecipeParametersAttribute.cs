using ProjectX.Domain.Entities;
using ProjectX.Domain.Enums;

namespace ProjectX.Domain.Attributes;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class CraftingRecipeParametersAttribute : Attribute
{
    public CraftingRecipeTypeEnum Type { get; set; }

    public required string Requirement { get; set; }

    public required string Reward { get; set; }

    public StatusEnum Status { get; set; } = StatusEnum.Active;
}
