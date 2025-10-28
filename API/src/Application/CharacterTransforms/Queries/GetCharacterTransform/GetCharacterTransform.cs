using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.Common.Interfaces;

namespace ProjectX.Application.CharacterTransforms.Queries.GetCharacterTransform;

public record class GetCharacterTransformQuery : IRequest<CharacterTransformDto>;

public class GetPlayerPositionQueryHandler : IRequestHandler<GetCharacterTransformQuery, CharacterTransformDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetPlayerPositionQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<CharacterTransformDto> Handle(GetCharacterTransformQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetId();

        return await _context.CharacterTransforms
            .Where(x => x.Character.ApplicationUserId == userId)
            .OrderByDescending(x => x.ModDate)
            .Select(x => new CharacterTransformDto
            {
                CharacterId = x.CharacterId,
                PositionX = x.PositionX,
                PositionY = x.PositionY,
                PositionZ = x.PositionZ,
                RotationY = x.RotationY
            })
            .FirstAsync(cancellationToken);
    }
}
