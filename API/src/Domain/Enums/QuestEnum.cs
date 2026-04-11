using System.Reflection;
using ProjectX.Domain.Attributes;

namespace ProjectX.Domain.Enums;

public enum QuestEnum : short
{
    None,

    [QuestParameters(
        Type = QuestTypeEnum.Kill,
        GameObjectName = "Bean(Clone)",
        Requirement = 2,
        Reward = 1000
    )]
    Kill2Beans,

    [QuestParameters(
        PreviousQuestId = Kill2Beans,
        Type = QuestTypeEnum.Collect,
        GameObjectName = nameof(InventoryItemEnum.Can),
        Requirement = 2,
        Reward = 1000
    )]
    Collect2Cans,

    [QuestParameters(
        PreviousQuestId = Kill2Beans,
        Type = QuestTypeEnum.Collect,
        GameObjectName = nameof(InventoryItemEnum.Fish),
        Requirement = 2,
        Reward = 1000
    )]
    Collect2Fish,

    [QuestParameters(
        Type = QuestTypeEnum.Collect,
        GameObjectName = nameof(InventoryItemEnum.PurpleOre),
        Requirement = 2,
        Reward = 1000
    )]
    Collect2PurpleOres,

    [QuestParameters(
        PreviousQuestId = Collect2PurpleOres,
        Type = QuestTypeEnum.Collect,
        GameObjectName = nameof(InventoryItemEnum.WhiteOre),
        Requirement = 2,
        Reward = 1000
    )]
    Collect2WhiteOres,

    [QuestParameters(
        PreviousQuestId = Collect2WhiteOres,
        Type = QuestTypeEnum.Collect,
        GameObjectName = nameof(InventoryItemEnum.CopperOre),
        Requirement = 2,
        Reward = 1000
    )]
    Collect2CopperOres,

    [QuestParameters(
        PreviousQuestId = Collect2CopperOres,
        Type = QuestTypeEnum.Collect,
        GameObjectName = nameof(InventoryItemEnum.BlackOre),
        Requirement = 2,
        Reward = 1000
    )]
    Collect2BlackOres,

    [QuestParameters(
        Type = QuestTypeEnum.Collect,
        GameObjectName = nameof(InventoryItemEnum.Chamomile),
        Requirement = 2,
        Reward = 1000
    )]
    Collect2Chamomile,

    [QuestParameters(
        Type = QuestTypeEnum.Collect,
        GameObjectName = nameof(InventoryItemEnum.Wood),
        Requirement = 2,
        Reward = 1000
    )]
    Collect2Woods,

    [QuestParameters(
        Type = QuestTypeEnum.Collect,
        GameObjectName = nameof(InventoryItemEnum.CookedFish),
        Requirement = 2,
        Reward = 1000
    )]
    Collect2CookedFish, // TODO: cook

    [QuestParameters(
        PreviousQuestId = Collect2CookedFish,
        Type = QuestTypeEnum.Collect,
        GameObjectName = nameof(InventoryItemEnum.Sushi),
        Requirement = 2,
        Reward = 1000
    )]
    Collect2Sushi, // TODO: cook
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
