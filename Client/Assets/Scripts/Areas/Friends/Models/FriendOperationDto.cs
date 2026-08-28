using System;
using Assets.Scripts.Areas.Friends.Enums;

namespace Assets.Scripts.Areas.Friends.Models
{
    [Serializable]
    public class FriendOperationDto
    {
        public FriendOperationStatusEnum Status { get; set; }

        public int CharacterId { get; set; }

        public string CharacterName { get; set; }
    }
}
