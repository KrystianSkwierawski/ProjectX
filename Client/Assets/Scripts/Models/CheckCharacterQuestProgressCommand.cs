using System;
using Assets.Scripts.Enums;

namespace Assets.Scripts.Models
{
    [Serializable]
    public class CheckCharacterQuestProgressCommand
    {
        public QuestEnum questId;

        public int progress;

        public int characterId;
    }
}