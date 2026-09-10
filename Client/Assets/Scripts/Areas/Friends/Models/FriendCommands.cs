namespace Assets.Scripts.Areas.Friends.Models
{
    public class SendFriendInvitationCommand
    {
        public string CharacterName { get; set; }
    }

    public class RespondFriendInvitationCommand
    {
        public int CharacterId { get; set; }

        public bool Accept { get; set; }
    }
}
