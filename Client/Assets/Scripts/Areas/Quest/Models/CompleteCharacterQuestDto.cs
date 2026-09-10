using Assets.Scripts.Areas.Quest.Enums;

namespace Assets.Scripts.Areas.Quest.Models
{
    public class CompleteCharacterQuestDto
    {
        public QuestEnum QuestId { get; set; }

        public CompleteCharacterQuestStatusEnum Status { get; set; }

        public int Reward { get; set; }

        public byte Level { get; set; }
    }

    public enum CompleteCharacterQuestStatusEnum
    {
        Applied,
        InventoryChanged
    }
}
