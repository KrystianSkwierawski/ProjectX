using System;
using Assets.Scripts.Enums;

namespace Assets.Scripts.Models
{
    [Serializable]
    public class AddCharacterQuestProgresDto
    {
        public CharacterQuestStatusEnum status;

        public int reward;
    }
}