using Assets.Scripts.Enums;

namespace Assets.Scripts.Models
{
    public class CheckCharacterQuestProgressDto
    {
        public QuestEnum QuestId { get; set; }

        public int CharacterQuestId { get; set; }

        public CharacterQuestStatusEnum Status { get; set; }
    }
}