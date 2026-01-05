using System;
using Assets.Scripts.Enums;

namespace Assets.Scripts.Models
{
    [Serializable]
    public class QuestDto
    {
        public QuestEnum id;

        public QuestEnum previousQuestId;

        public QuestTypeEnum type;

        public string title;

        public string description;

        public string completeDescription;

        public string statusText;

        public string gameObjectName;

        public int requirement;

        public int reward;
    }
}