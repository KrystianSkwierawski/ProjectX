using ProjectX.Domain.Enums;

namespace ProjectX.Domain.Attributes;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public sealed class QuestItemRequirementAttribute(InventoryItemEnum type, int count) : Attribute
{
    public InventoryItemEnum Type { get; } = type;

    public int Count { get; } = count;
}
