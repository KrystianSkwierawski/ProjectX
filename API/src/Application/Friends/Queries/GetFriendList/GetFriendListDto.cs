namespace ProjectX.Application.Friends.Queries.GetFriendList;

public sealed class GetFriendListDto
{
    public IReadOnlyCollection<FriendDto> Friends { get; init; } = [];

    public IReadOnlyCollection<FriendInvitationDto> IncomingInvitations { get; init; } = [];

    public IReadOnlyCollection<FriendInvitationDto> OutgoingInvitations { get; init; } = [];
}
