using System.Collections.Generic;

namespace Assets.Scripts.Areas.Quest.Models
{
    public class GetCharacterQuestsDto
    {
        public IList<CharacterQuestDto> CharacterQuests { get; set; }
    }
}