using System;

namespace Assets.Scripts.Areas.Party.Models
{
    [Serializable]
    public class PartySnapshotDto
    {
        public PartyMemberDto[] Members { get; set; } = Array.Empty<PartyMemberDto>();

        public PartyInvitationDto[] Invitations { get; set; } = Array.Empty<PartyInvitationDto>();
    }
}
