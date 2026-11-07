namespace ProjectX.Domain.Attributes;

[AttributeUsage(AttributeTargets.Field)]
public sealed class QuestExperienceRewardAttribute(int amount) : Attribute
{
    public int Amount { get; } = amount;
}
