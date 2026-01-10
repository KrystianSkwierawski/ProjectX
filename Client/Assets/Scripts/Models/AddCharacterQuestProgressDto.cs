using System;
using Assets.Scripts.Enums;

namespace Assets.Scripts.Models
{
    [Serializable]
    public class AddCharacterQuestProgressDto
    {
        public CharacterQuestStatusEnum status;

        public int reward;
    }
}