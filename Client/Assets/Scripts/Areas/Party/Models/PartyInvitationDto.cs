using System;

namespace Assets.Scripts.Areas.Party.Models
{
    [Serializable]
    public class PartyInvitationDto
    {
        public int CharacterId { get; set; }

        public string CharacterName { get; set; }
    }
}
