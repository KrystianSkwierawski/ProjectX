using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;
using Assets.Scripts.Areas.Quest.Enums;

namespace Assets.Scripts.Areas.Quest.Models
{
    public class QuestDto
    {
        public QuestEnum Id { get; set; }

        public QuestEnum PreviousQuestId { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string CompleteDescription { get; set; }

        public string StatusText { get; set; }

        public QuestRequirementDto[] Requirements { get; set; } = System.Array.Empty<QuestRequirementDto>();

        public int Reward { get; set; }

        public InventoryItemDto[] ItemRewards { get; set; } = System.Array.Empty<InventoryItemDto>();
    }
    public class QuestRequirementDto
    {
        public string Id { get; set; }

        public QuestTypeEnum Type { get; set; }

        public string Target { get; set; }

        public InventoryItemEnum ItemType { get; set; }

        public int Count { get; set; }

        public string StatusText { get; set; }
    }
}