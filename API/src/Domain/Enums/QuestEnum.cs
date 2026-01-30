using System.Reflection;
using ProjectX.Domain.Attributes;

namespace ProjectX.Domain.Enums;

public enum QuestEnum : short
{
    None,

    [QuestParameters(
        Type = QuestTypeEnum.Kill,
        TitleKey = TranslateKeyEnum.Kill2BeansTitle,
        Description = TranslateKeyEnum.Kill2BeansDescription,
        CompleteDescription = TranslateKeyEnum.Kill2BeansCompleteDescription,
        StatusText = TranslateKeyEnum.Kill2BeansStatusText,
        GameObjectName = "Bean(Clone)",
        Requirement = 2,
        Reward = 1000
    )]
    Kill2Beans,

    [QuestParameters(
        PreviousQuestId = Kill2Beans,
        Type = QuestTypeEnum.Collect,
        TitleKey = TranslateKeyEnum.Collect2CansTitle,
        Description = TranslateKeyEnum.Collect2CansDescription,
        CompleteDescription = TranslateKeyEnum.Collect2CansCompleteDescription,
        StatusText = TranslateKeyEnum.Collect2CansStatusText,
        GameObjectName = "Can",
        Requirement = 2,
        Reward = 1000
    )]
    Collect2Cans,

    [QuestParameters(
        PreviousQuestId = Kill2Beans,
        Type = QuestTypeEnum.Collect,
        TitleKey = TranslateKeyEnum.Catch2FishsesTitle,
        Description = TranslateKeyEnum.Catch2FishsesDescription,
        CompleteDescription = TranslateKeyEnum.Catch2FishsesCompleteDescription,
        StatusText = TranslateKeyEnum.Catch2FishsesStatusText,
        GameObjectName = "Fish",
        Requirement = 2,
        Reward = 1000
    )]
    Catch2Fishses
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
