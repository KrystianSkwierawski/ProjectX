using System;

namespace Assets.Scripts.Areas.Friends.Models
{
    [Serializable]
    public class FriendDto
    {
        public int CharacterId { get; set; }

        public string CharacterName { get; set; }

        public byte Level { get; set; }

        public bool IsOnline { get; set; }
    }
}
