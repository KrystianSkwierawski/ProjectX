using ProjectX.Application.Friends;

namespace ProjectX.Application.Friends.Queries.AuthorizeWhisper;

public sealed class AuthorizeWhisperDto
{
    public FriendOperationStatusEnum Status { get; init; }

    public int CharacterId { get; init; }

    public string CharacterName { get; init; } = string.Empty;

    public bool IsAllowed { get; init; }
}
