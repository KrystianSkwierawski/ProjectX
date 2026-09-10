using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.Common.Extensions;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.Friends.Queries.AuthorizeWhisper;

public record AuthorizeWhisperQuery(int CharacterId) : IRequest<AuthorizeWhisperDto>;

public class AuthorizeWhisperQueryHandler : IRequestHandler<AuthorizeWhisperQuery, AuthorizeWhisperDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public AuthorizeWhisperQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<AuthorizeWhisperDto> Handle(AuthorizeWhisperQuery request, CancellationToken cancellationToken)
    {
        var characterId = _currentUserService.GetRequiredCharacterId();

        var target = await _context.Characters
            .Where(x => x.Id == request.CharacterId)
            .Where(x => x.Status == StatusEnum.Active)
            .Select(x => new { x.Id, x.Name })
            .SingleOrDefaultAsync(cancellationToken);

        if (target is null)
        {
            return new AuthorizeWhisperDto
            {
                Status = FriendOperationStatusEnum.CharacterNotFound,
                CharacterId = request.CharacterId
            };
        }

        var firstCharacterId = Math.Min(characterId, request.CharacterId);
        var secondCharacterId = Math.Max(characterId, request.CharacterId);

        var isFriend = await _context.CharacterFriendships
            .Where(x => x.FirstCharacterId == firstCharacterId)
            .Where(x => x.SecondCharacterId == secondCharacterId)
            .AnyAsync(x => x.Status == FriendshipStatusEnum.Accepted, cancellationToken);

        return new AuthorizeWhisperDto
        {
            Status = isFriend ? FriendOperationStatusEnum.Applied : FriendOperationStatusEnum.WhisperNotAllowed,
            CharacterId = target.Id,
            CharacterName = target.Name,
            IsAllowed = isFriend
        };
    }
}
