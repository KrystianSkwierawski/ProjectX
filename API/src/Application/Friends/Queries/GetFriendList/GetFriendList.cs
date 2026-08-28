using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.Common.Extensions;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Entities;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.Friends.Queries.GetFriendList;

public record GetFriendListQuery : IRequest<GetFriendListDto>;

public class GetFriendListQueryHandler : IRequestHandler<GetFriendListQuery, GetFriendListDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IGameSessionService _gameSessionService;

    public GetFriendListQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IGameSessionService gameSessionService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _gameSessionService = gameSessionService;
    }

    public async Task<GetFriendListDto> Handle(GetFriendListQuery request, CancellationToken cancellationToken)
    {
        var characterId = _currentUserService.GetRequiredCharacterId();
        var serverUserId = _currentUserService.GetAuthenticatedUserId();

        var relationships = await _context.CharacterFriendships
            .Where(x => x.FirstCharacterId == characterId || x.SecondCharacterId == characterId)
            .ToListAsync(cancellationToken);

        var relatedCharacterIds = relationships
            .Select(x => x.GetOtherCharacterId(characterId))
            .Distinct()
            .ToArray();

        var characters = await _context.Characters
            .Where(x => relatedCharacterIds.Contains(x.Id))
            .Where(x => x.Status == StatusEnum.Active)
            .Select(x => new CharacterLookup(x.Id, x.Name))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var friends = relationships
            .Where(x => x.Status == FriendshipStatusEnum.Accepted)
            .Select(x => x.GetOtherCharacterId(characterId))
            .Where(characters.ContainsKey)
            .Select(x => new FriendDto
            {
                CharacterId = x,
                CharacterName = characters[x].Name,
                IsOnline = _gameSessionService.IsCharacterOnline(serverUserId, x)
            })
            .OrderByDescending(x => x.IsOnline)
            .ThenBy(x => x.CharacterName)
            .ToArray();

        var incomingInvitations = CreateInvitations(
            relationships.Where(x => x.Status == FriendshipStatusEnum.Pending && x.RequestedByCharacterId != characterId),
            characterId,
            characters);

        var outgoingInvitations = CreateInvitations(
            relationships.Where(x => x.Status == FriendshipStatusEnum.Pending && x.RequestedByCharacterId == characterId),
            characterId,
            characters);

        return new GetFriendListDto
        {
            Friends = friends,
            IncomingInvitations = incomingInvitations,
            OutgoingInvitations = outgoingInvitations
        };
    }

    private static IReadOnlyCollection<FriendInvitationDto> CreateInvitations(
        IEnumerable<CharacterFriendship> relationships,
        int characterId,
        IReadOnlyDictionary<int, CharacterLookup> characters)
    {
        return relationships
            .Select(x => x.GetOtherCharacterId(characterId))
            .Where(characters.ContainsKey)
            .Select(x => new FriendInvitationDto
            {
                CharacterId = x,
                CharacterName = characters[x].Name
            })
            .OrderBy(x => x.CharacterName)
            .ToArray();
    }

    private sealed record CharacterLookup(int Id, string Name);
}
