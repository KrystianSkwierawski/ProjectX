using ProjectX.Domain.Enums;

namespace ProjectX.Domain.Quests;

public sealed record QuestRequirement(string Id, QuestTypeEnum Type, string Target, int Count,
    InventoryItemEnum ItemType = InventoryItemEnum.None, string StatusTextKey = "");
