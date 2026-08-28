using ProjectX.Application.Friends;

namespace ProjectX.Application.Friends.Commands.RespondFriendInvitation;

public sealed class RespondFriendInvitationDto
{
    public FriendOperationStatusEnum Status { get; init; }

    public int CharacterId { get; init; }

    public string CharacterName { get; init; } = string.Empty;
}
