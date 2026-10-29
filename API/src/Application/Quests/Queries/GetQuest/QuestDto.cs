using ProjectX.Application.CharacterInventories.Queries.GetCharacterInventory;
using ProjectX.Domain.Enums;
using ProjectX.Domain.Entities;
using ProjectX.Application.Common.Interfaces;

namespace ProjectX.Application.Quests.Queries.GetQuest;

public class QuestDto
{
    public QuestEnum Id { get; set; }

    public QuestEnum PreviousQuestId { get; set; }

    public required string Title { get; set; }

    public required string Description { get; set; }

    public required string CompleteDescription { get; set; }

    public required string StatusText { get; set; }

    public QuestRequirementDto[] Requirements { get; set; } = [];

    public int Reward { get; set; }

    public InventoryItemDto[] ItemRewards { get; set; } = System.Array.Empty<InventoryItemDto>();

    public static QuestDto From(Quest quest, ITranslateService translations, LanguageEnum language)
    {
        return new QuestDto
        {
            Id = quest.Id,
            PreviousQuestId = quest.PreviousQuestId,
            Title = translations.GetByKey($"{quest.Id}Title", language),
            Description = translations.GetByKey($"{quest.Id}Description", language),
            CompleteDescription = translations.GetByKey($"{quest.Id}CompleteDescription", language),
            StatusText = translations.GetByKey($"{quest.Id}StatusText", language),
            Reward = quest.Id.GetExperienceReward(),
            Requirements = quest.Id.GetRequirements()
                .Select(x => new QuestRequirementDto
                {
                    Id = x.Id,
                    Type = x.Type,
                    Target = x.Target,
                    Count = x.Count,
                    ItemType = x.ItemType,
                    StatusText = x.Type == QuestTypeEnum.Kill ? translations.GetByKey(x.StatusTextKey, language)
                        : translations.GetByKey(x.ItemType + "Title", language)
                })
                .ToArray(),
            ItemRewards = quest.Id.GetItemRewards()
                .Select(x => new InventoryItemDto { Type = x.Type, Count = x.Count })
                .ToArray()
        };
    }

    public override string ToString() => $"Quest {Id}, Requirements={Requirements.Length}, ItemRewards={ItemRewards.Length}";
}

public class QuestRequirementDto
{
    public required string Id { get; set; }

    public QuestTypeEnum Type { get; set; }

    public required string Target { get; set; }

    public InventoryItemEnum ItemType { get; set; }

    public int Count { get; set; }

    public required string StatusText { get; set; }
}
