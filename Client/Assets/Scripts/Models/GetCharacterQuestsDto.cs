using System;
using System.Collections.Generic;

namespace Assets.Scripts.Models
{
    [Serializable]
    public class GetCharacterQuestsDto
    {
        public List<CharacterQuestDto> characterQuests = new List<CharacterQuestDto>();
    }
}