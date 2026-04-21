using Assets.Scripts.Enums;

namespace Assets.Scripts.Models
{
    public class QuestDto
    {
        public QuestEnum Id { get; set; }

        public QuestEnum PreviousQuestId { get; set; }

        public QuestTypeEnum Type { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string CompleteDescription { get; set; }

        public string StatusText { get; set; }

        public string GameObjectName { get; set; }

        public int Requirement { get; set; }

        public int Reward { get; set; }
    }
}