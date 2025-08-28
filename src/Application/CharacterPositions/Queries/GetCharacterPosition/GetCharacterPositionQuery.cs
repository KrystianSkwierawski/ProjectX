using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.Common.Interfaces;

namespace ProjectX.Application.PlayerPositions.Queries.GetPlayerPosition;

public record class GetCharacterPositionQuery : IRequest<CharacterPositionDto>;

public class GetPlayerPositionQueryHandler : IRequestHandler<GetCharacterPositionQuery, CharacterPositionDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetPlayerPositionQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<CharacterPositionDto> Handle(GetCharacterPositionQuery request, CancellationToken cancellationToken)
    {
        return await _context.CharacterPosition
            .Where(x => x.Character.ApplicationUserId == _currentUserService.Id)
            .OrderByDescending(x => x.ModDate)
            .Select(x => new CharacterPositionDto
            {
                CharacterId = x.CharacterId,
                X = x.X,
                Y = x.Y,
                Z = x.Z
            })
            .FirstAsync(cancellationToken);
    }
}
