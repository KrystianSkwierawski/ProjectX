using Assets.Scripts.Areas.Quest.Enums;

namespace Assets.Scripts.Areas.Quest.Models
{
    public class CheckCharacterQuestProgressDto
    {
        public QuestEnum QuestId { get; set; }

        public int CharacterQuestId { get; set; }

        public CharacterQuestStatusEnum Status { get; set; }
    }
}