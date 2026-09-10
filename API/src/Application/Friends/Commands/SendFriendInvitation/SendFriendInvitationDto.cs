using ProjectX.Application.Friends;

namespace ProjectX.Application.Friends.Commands.SendFriendInvitation;

public sealed class SendFriendInvitationDto
{
    public FriendOperationStatusEnum Status { get; init; }

    public int CharacterId { get; init; }

    public string CharacterName { get; init; } = string.Empty;
}
