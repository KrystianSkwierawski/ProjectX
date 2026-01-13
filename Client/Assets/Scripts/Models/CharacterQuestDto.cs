using System;
using Assets.Scripts.Enums;

namespace Assets.Scripts.Models
{
    [Serializable]
    public class CharacterQuestDto
    {
        public int id;

        public QuestEnum questId;

        public CharacterQuestStatusEnum status;

        public int progress;
    }
}