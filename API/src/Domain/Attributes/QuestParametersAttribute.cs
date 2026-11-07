using ProjectX.Domain.Enums;

namespace ProjectX.Domain.Attributes;

[AttributeUsage(AttributeTargets.Field)]
public class QuestParametersAttribute : Attribute
{
    public QuestEnum PreviousQuestId { get; set; }

    public StatusEnum Status { get; set; } = StatusEnum.Active;
}
