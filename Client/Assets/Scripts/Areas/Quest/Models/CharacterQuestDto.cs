using System.Collections.Generic;
using Assets.Scripts.Areas.Quest.Enums;

namespace Assets.Scripts.Areas.Quest.Models
{
    public class CharacterQuestDto
    {
        public int Id { get; set; }

        public QuestEnum QuestId { get; set; }

        public CharacterQuestStatusEnum Status { get; set; }

        public int Progress { get; set; }

        public Dictionary<string, int> ObjectiveProgress { get; set; } = new Dictionary<string, int>();
    }
}