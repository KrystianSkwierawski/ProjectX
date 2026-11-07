using System.Collections.Generic;
using Assets.Scripts.Areas.Quest.Enums;

namespace Assets.Scripts.Areas.Quest.Models
{
    public class CheckCharacterQuestProgressDto
    {
        public QuestEnum QuestId { get; set; }

        public int CharacterQuestId { get; set; }

        public int Progress { get; set; }

        public Dictionary<string, int> ObjectiveProgress { get; set; } = new Dictionary<string, int>();

        public CharacterQuestStatusEnum Status { get; set; }
    }
}
