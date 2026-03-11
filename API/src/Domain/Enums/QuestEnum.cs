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
        GameObjectName = nameof(InventoryItemEnum.Can),
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
        GameObjectName = nameof(InventoryItemEnum.Fish),
        Requirement = 2,
        Reward = 1000
    )]
    Catch2Fishses,

    [QuestParameters(
        Type = QuestTypeEnum.Collect,
        TitleKey = TranslateKeyEnum.Collect2PurpleOresTitle,
        Description = TranslateKeyEnum.Collect2PurpleOresDescription,
        CompleteDescription = TranslateKeyEnum.Collect2PurpleOresCompleteDescription,
        StatusText = TranslateKeyEnum.Collect2PurpleOresStatusText,
        GameObjectName = nameof(InventoryItemEnum.PurpleOre),
        Requirement = 2,
        Reward = 1000
    )]
    Collect2PurpleOres,

    [QuestParameters(
        PreviousQuestId = Collect2PurpleOres,
        Type = QuestTypeEnum.Collect,
        TitleKey = TranslateKeyEnum.Collect2WhiteOresTitle,
        Description = TranslateKeyEnum.Collect2WhiteOresDescription,
        CompleteDescription = TranslateKeyEnum.Collect2WhiteOresCompleteDescription,
        StatusText = TranslateKeyEnum.Collect2WhiteOresStatusText,
        GameObjectName = nameof(InventoryItemEnum.WhiteOre),
        Requirement = 2,
        Reward = 1000
    )]
    Collect2WhiteOres,

    [QuestParameters(
        PreviousQuestId = Collect2WhiteOres,
        Type = QuestTypeEnum.Collect,
        TitleKey = TranslateKeyEnum.Collect2CopperOresTitle,
        Description = TranslateKeyEnum.Collect2CopperOresDescription,
        CompleteDescription = TranslateKeyEnum.Collect2CopperOresDescription,
        StatusText = TranslateKeyEnum.Collect2CopperOresStatusText,
        GameObjectName = nameof(InventoryItemEnum.CopperOre),
        Requirement = 2,
        Reward = 1000
    )]
    Collect2CopperOres,

    [QuestParameters(
        PreviousQuestId = Collect2CopperOres,
        Type = QuestTypeEnum.Collect,
        TitleKey = TranslateKeyEnum.Collect2BlackOresTitle,
        Description = TranslateKeyEnum.Collect2BlackOresDescription,
        CompleteDescription = TranslateKeyEnum.Collect2BlackOresCompleteDescription,
        StatusText = TranslateKeyEnum.Collect2BlackOresStatusText,
        GameObjectName = nameof(InventoryItemEnum.BlackOre),
        Requirement = 2,
        Reward = 1000
    )]
    Collect2BlackOres,

    [QuestParameters(
        Type = QuestTypeEnum.Collect,
        TitleKey = TranslateKeyEnum.Collect2ChamomileTitle,
        Description = TranslateKeyEnum.Collect2ChamomileDescription,
        CompleteDescription = TranslateKeyEnum.Collect2ChamomileCompleteDescription,
        StatusText = TranslateKeyEnum.Collect2ChamomileStatusText,
        GameObjectName = nameof(InventoryItemEnum.Chamomile),
        Requirement = 2,
        Reward = 1000
    )]
    Collect2Chamomile,

    [QuestParameters(
        Type = QuestTypeEnum.Collect,
        TitleKey = TranslateKeyEnum.Collect2WoodsTitle,
        Description = TranslateKeyEnum.Collect2WoodsDescription,
        CompleteDescription = TranslateKeyEnum.Collect2WoodsCompleteDescription,
        StatusText = TranslateKeyEnum.Collect2WoodsStatusText,
        GameObjectName = nameof(InventoryItemEnum.Wood),
        Requirement = 2,
        Reward = 1000
    )]
    Collect2Woods,
}

public static class QuestEnumExtensions
{
    public static QuestParametersAttribute GetParameters(this QuestEnum value)
    {
        var member = value
            .GetType()
            .GetMember(value.ToString())
            .First();

        return member.GetCustomAttribute<QuestParametersAttribute>() ?? throw new ArgumentNullException(nameof(value));
    }
}
