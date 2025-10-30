using System;
using Assets.Scripts.Enums;

namespace Assets.Scripts.Models
{
    [Serializable]
    public class CheckCharacterQuestProgresDto
    {
        public int questId;

        public int characterQuestId;

        public CharacterQuestStatusEnum status;
    }
}