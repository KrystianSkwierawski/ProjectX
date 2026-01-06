using System.Reflection;
using ProjectX.Domain.Attributes;

namespace ProjectX.Domain.Enums;

public enum QuestEnum : short
{
    None,

    [QuestParameters(
        Type = QuestTypeEnum.Kill,
        Title = "Quest.Kill2Beans.Title",
        Description = "Quest.Kill2Beans.Description",
        CompleteDescription = "Quest.Kill2Beans.CompleteDescription",
        StatusText = "Quest.Kill2Beans.StatusText",
        GameObjectName = "Bean(Clone)",
        Requirement = 2,
        Reward = 1000
    )]
    Kill2Beans,

    [QuestParameters(
        PreviousQuestId = Kill2Beans,
        Type = QuestTypeEnum.Collect,
        Title = "Quest.Collect2Cans.Title",
        Description = "Quest.Collect2Cans.Description",
        CompleteDescription = "Quest.Collect2Cans.CompleteDescription",
        StatusText = "Quest.Collect2Cans.StatusText",
        GameObjectName = "Can",
        Requirement = 2,
        Reward = 1000
    )]
    Collect2Cans
}

public static class QuestEnumExtensions
{
    public static QuestParametersAttribute GetQuestParametersAttribute(this QuestEnum value)
    {
        var member = value
            .GetType()
            .GetMember(value.ToString())
            .First();

        return member.GetCustomAttribute<QuestParametersAttribute>() ?? throw new ArgumentNullException(nameof(value));
    }
}
