namespace ProjectX.Application.Friends.Queries.GetFriendList;

public sealed class FriendDto
{
    public int CharacterId { get; init; }

    public required string CharacterName { get; init; }

    public byte Level { get; init; }

    public bool IsOnline { get; init; }
}
