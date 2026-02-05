using ProjectX.Domain.Enums;

namespace ProjectX.Domain.Attributes;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class QuestParametersAttribute : Attribute
{
    public QuestEnum PreviousQuestId { get; set; }

    public QuestTypeEnum Type { get; set; }

    public required TranslateKeyEnum TitleKey { get; set; }

    public required TranslateKeyEnum Description { get; set; }

    public required TranslateKeyEnum CompleteDescription { get; set; }

    public required TranslateKeyEnum StatusText { get; set; }

    public required string GameObjectName { get; set; }

    public int Requirement { get; set; }

    public int Reward { get; set; }

    public StatusEnum Status { get; set; } = StatusEnum.Active;
}
