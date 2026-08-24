using Assets.Scripts.Areas.Quest.Enums;

namespace Assets.Scripts.Areas.Quest.Models
{
    public class CheckCharacterQuestProgressCommand
    {
        public QuestEnum QuestId { get; set; }

        public int Progress { get; set; }
    }
}
