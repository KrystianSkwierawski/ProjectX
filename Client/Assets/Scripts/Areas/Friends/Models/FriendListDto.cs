using System;

namespace Assets.Scripts.Areas.Friends.Models
{
    [Serializable]
    public class FriendListDto
    {
        public FriendDto[] Friends { get; set; } = Array.Empty<FriendDto>();

        public FriendInvitationDto[] IncomingInvitations { get; set; } = Array.Empty<FriendInvitationDto>();

        public FriendInvitationDto[] OutgoingInvitations { get; set; } = Array.Empty<FriendInvitationDto>();
    }
}
