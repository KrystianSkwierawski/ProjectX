using System;
using Assets.Scripts.Enums;

namespace Assets.Scripts.Models
{
    [Serializable]
    public class AddCharacterExperienceCommand
    {
        public int characterId;

        public int characterQuestId;

        public ExperienceTypeEnum type;
    }
}