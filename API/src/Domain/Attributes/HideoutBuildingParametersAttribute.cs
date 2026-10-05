using ProjectX.Domain.Crafting;
using ProjectX.Domain.Enums;

namespace ProjectX.Domain.Attributes;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class HideoutBuildingParametersAttribute : Attribute
{
    public HideoutBuildingParametersAttribute(InventoryItemEnum[] items, int[] counts)
    {
        if (items.Length != counts.Length)
        {
            throw new ArgumentException("Each required item must have a count.", nameof(counts));
        }

        Requirement = new(items.Select((item, index) => new CraftingRecipeItem(item, counts[index])).ToArray(), 0);
    }

    public CraftingRecipeRequirement Requirement { get; }

    public int BuildTime { get; set; }

    public StatusEnum Status { get; set; } = StatusEnum.Active;
}
