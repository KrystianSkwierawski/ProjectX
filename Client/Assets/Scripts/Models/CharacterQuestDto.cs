using Assets.Scripts.Enums;

namespace Assets.Scripts.Models
{
    public class CharacterQuestDto
    {
        public int Id { get; set; }

        public QuestEnum QuestId { get; set; }

        public CharacterQuestStatusEnum Status { get; set; }

        public int Progress { get; set; }
    }
}