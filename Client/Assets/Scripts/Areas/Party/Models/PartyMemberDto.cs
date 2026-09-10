using System;

namespace Assets.Scripts.Areas.Party.Models
{
    [Serializable]
    public class PartyMemberDto
    {
        public int CharacterId { get; set; }

        public string CharacterName { get; set; }

        public int Health { get; set; }

        public int MaxHealth { get; set; }

        public byte Level { get; set; }

        public bool IsLeader { get; set; }
    }
}
