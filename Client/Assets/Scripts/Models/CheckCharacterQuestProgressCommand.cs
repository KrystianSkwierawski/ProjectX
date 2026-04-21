using Assets.Scripts.Enums;

namespace Assets.Scripts.Models
{
    public class CheckCharacterQuestProgressCommand
    {
        public QuestEnum QuestId { get; set; }

        public int Progress { get; set; }

        public int CharacterId { get; set; }
    }
}