using System;
using Assets.Scripts.Enums;

namespace Assets.Scripts.Models
{
    [Serializable]
    public class CheckCharacterQuestProgresDto
    {
        public QuestEnum questId;

        public int characterQuestId;

        public CharacterQuestStatusEnum status;
    }
}