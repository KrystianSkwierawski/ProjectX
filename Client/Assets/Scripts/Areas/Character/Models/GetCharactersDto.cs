using System;
using System.Collections.Generic;

namespace Assets.Scripts.Areas.Character.Models
{
    public sealed class GetCharactersDto
    {
        public IList<CharacterSummaryDto> Characters { get; set; } = Array.Empty<CharacterSummaryDto>();
    }
}
