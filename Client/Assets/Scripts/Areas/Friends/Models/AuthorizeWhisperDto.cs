using System;
using Assets.Scripts.Areas.Friends.Enums;

namespace Assets.Scripts.Areas.Friends.Models
{
    [Serializable]
    public class AuthorizeWhisperDto
    {
        public FriendOperationStatusEnum Status { get; set; }

        public int CharacterId { get; set; }

        public string CharacterName { get; set; }

        public bool IsAllowed { get; set; }
    }
}
