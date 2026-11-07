using System.Reflection;
using ProjectX.Domain.Quests;
using ProjectX.Domain.Attributes;

namespace ProjectX.Domain.Enums;

public enum QuestEnum : short
{
    None,

    [QuestParameters]
    [QuestKillRequirement("Bean(Clone)", 2, "Kill2BeansStatusText")]
    [QuestExperienceReward(1000)]
    [QuestItemReward(InventoryItemEnum.Currency, 25)]
    [QuestItemReward(InventoryItemEnum.HealthPotion, 2)]
    Kill2Beans,

    [QuestParameters(PreviousQuestId = Kill2Beans)]
    [QuestItemRequirement(InventoryItemEnum.Can, 2)]
    [QuestExperienceReward(1000)]
    [QuestItemReward(InventoryItemEnum.Currency, 15)]
    Collect2Cans,

    [QuestParameters(PreviousQuestId = Kill2Beans)]
    [QuestItemRequirement(InventoryItemEnum.Fish, 2)]
    [QuestExperienceReward(1000)]
    Collect2Fish,

    [QuestParameters]
    [QuestItemRequirement(InventoryItemEnum.PurpleOre, 2)]
    [QuestExperienceReward(1000)]
    Collect2PurpleOres,

    [QuestParameters(PreviousQuestId = Collect2PurpleOres)]
    [QuestItemRequirement(InventoryItemEnum.WhiteOre, 2)]
    [QuestExperienceReward(1000)]
    Collect2WhiteOres,

    [QuestParameters(PreviousQuestId = Collect2WhiteOres)]
    [QuestItemRequirement(InventoryItemEnum.CopperOre, 2)]
    [QuestExperienceReward(1000)]
    Collect2CopperOres,

    [QuestParameters(PreviousQuestId = Collect2CopperOres)]
    [QuestItemRequirement(InventoryItemEnum.BlackOre, 2)]
    [QuestExperienceReward(1000)]
    Collect2BlackOres,

    [QuestParameters]
    [QuestItemRequirement(InventoryItemEnum.Chamomile, 2)]
    [QuestExperienceReward(1000)]
    Collect2Chamomile,

    [QuestParameters]
    [QuestItemRequirement(InventoryItemEnum.Wood, 2)]
    [QuestExperienceReward(1000)]
    Collect2Woods,

    [QuestParameters]
    [QuestItemRequirement(InventoryItemEnum.CookedFish, 2)]
    [QuestExperienceReward(1000)]
    Collect2CookedFish,

    [QuestParameters(PreviousQuestId = Collect2CookedFish)]
    [QuestItemRequirement(InventoryItemEnum.Sushi, 2)]
    [QuestExperienceReward(1000)]
    Collect2Sushi,

    [QuestParameters]
    [QuestItemRequirement(InventoryItemEnum.PurpleBar, 2)]
    [QuestExperienceReward(1000)]
    Collect2PurpleBars,

    [QuestParameters(PreviousQuestId = Collect2PurpleBars)]
    [QuestItemRequirement(InventoryItemEnum.WhiteBar, 2)]
    [QuestExperienceReward(1000)]
    Collect2WhiteBars,

    [QuestParameters(PreviousQuestId = Collect2WhiteBars)]
    [QuestItemRequirement(InventoryItemEnum.CopperBar, 2)]
    [QuestExperienceReward(1000)]
    Collect2CopperBars,

    [QuestParameters(PreviousQuestId = Collect2CopperBars)]
    [QuestItemRequirement(InventoryItemEnum.BlackBar, 2)]
    [QuestExperienceReward(1000)]
    Collect2BlackBars,

    [QuestParameters(PreviousQuestId = Collect2CopperBars)]
    [QuestItemRequirement(InventoryItemEnum.HealthPotion, 2)]
    [QuestExperienceReward(1000)]
    Collect2HealthPotions,
    [QuestParameters(PreviousQuestId = Collect2Cans)]
    [QuestKillRequirement("Bean(Clone)", 2, "Kill2BeansStatusText")]
    [QuestItemRequirement(InventoryItemEnum.Can, 2)]
    [QuestItemRequirement(InventoryItemEnum.Fish, 1)]
    [QuestExperienceReward(1500)]
    [QuestItemReward(InventoryItemEnum.Currency, 40)]
    [QuestItemReward(InventoryItemEnum.HealthPotion, 3)]
    FieldSupplies,

}

public static class QuestEnumExtensions
{
    public static QuestRequirement[] GetRequirements(this QuestEnum value)
    {
        var member = typeof(QuestEnum)
            .GetMember(value.ToString())
            .Single();

        var kills = member.GetCustomAttributes<QuestKillRequirementAttribute>()
            .Select(x => new QuestRequirement("kill:" + x.Target, QuestTypeEnum.Kill, x.Target, x.Count,
                StatusTextKey: x.StatusTextKey));

        var items = member.GetCustomAttributes<QuestItemRequirementAttribute>()
            .Select(x => new QuestRequirement("item:" + (int)x.Type, QuestTypeEnum.Collect, x.Type.ToString(), x.Count, x.Type));

        var requirements = kills
            .Concat(items)
            .ToArray();

        if (requirements.Length == 0 || requirements.Any(x => x.Count <= 0)
            || requirements
                .Select(x => x.Id)
                .Distinct()
                .Count() != requirements.Length)
        {
            throw new InvalidOperationException($"Invalid requirements for quest {value}.");
        }

        return requirements;
    }

    public static int GetExperienceReward(this QuestEnum value)
    {
        return typeof(QuestEnum)
            .GetMember(value.ToString())
            .Single()
            .GetCustomAttribute<QuestExperienceRewardAttribute>()?.Amount ?? 0;
    }

    public static QuestItemRewardAttribute[] GetItemRewards(this QuestEnum value)
    {
        return typeof(QuestEnum)
            .GetMember(value.ToString())
            .Single()
            .GetCustomAttributes<QuestItemRewardAttribute>()
            .ToArray();
    }

    public static QuestParametersAttribute GetParameters(this QuestEnum value)
    {
        var member = value
            .GetType()
            .GetMember(value.ToString())
            .First();

        return member.GetCustomAttribute<QuestParametersAttribute>() ?? throw new ArgumentNullException(nameof(value));
    }
}
