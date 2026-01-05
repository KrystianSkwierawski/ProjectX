using ProjectX.Domain.Enums;

namespace ProjectX.Domain.Attributes;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class QuestParametersAttribute : Attribute
{
    public QuestEnum PreviousQuestId { get; set; }

    public QuestTypeEnum Type { get; set; }

    public string Title { get; set; }

    public string Description { get; set; }

    public string CompleteDescription { get; set; }

    public string StatusText { get; set; }

    public string GameObjectName { get; set; }

    public int Requirement { get; set; }

    public int Reward { get; set; }

    public StatusEnum Active { get; set; } = StatusEnum.Active;
}
