using System;

namespace Assets.Scripts.Areas.Friends.Models
{
    [Serializable]
    public class FriendInvitationDto
    {
        public int CharacterId { get; set; }

        public string CharacterName { get; set; }
    }
}
