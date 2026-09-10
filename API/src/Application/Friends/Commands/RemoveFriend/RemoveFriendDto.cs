using ProjectX.Application.Friends;

namespace ProjectX.Application.Friends.Commands.RemoveFriend;

public sealed class RemoveFriendDto
{
    public FriendOperationStatusEnum Status { get; init; }

    public int CharacterId { get; init; }

    public string CharacterName { get; init; } = string.Empty;
}
