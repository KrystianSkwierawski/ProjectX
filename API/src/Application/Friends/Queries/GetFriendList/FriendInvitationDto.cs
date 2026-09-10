namespace ProjectX.Application.Friends.Queries.GetFriendList;

public sealed class FriendInvitationDto
{
    public int CharacterId { get; init; }

    public required string CharacterName { get; init; }
}
