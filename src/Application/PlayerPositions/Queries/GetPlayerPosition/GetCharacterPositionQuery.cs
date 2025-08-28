using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.Common.Attributes;

namespace ProjectX.Application.PlayerPositions.Queries.GetPlayerPosition;

public record class GetCharacterPositionQuery(int PlayerId) : IRequest<CharacterPositionDto>;

public class GetPlayerPositionQueryHandler : IRequestHandler<GetCharacterPositionQuery, CharacterPositionDto>
{
    private readonly IApplicationDbContext _context;

    public GetPlayerPositionQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CharacterPositionDto> Handle(GetCharacterPositionQuery request, CancellationToken cancellationToken)
    {
        return await _context.CharacterPosition
            .Where(x => x.CharacterId == request.PlayerId)
            .OrderByDescending(x => x.ModDate)
            .Select(x => new CharacterPositionDto
            {
                X = x.X,
                Y = x.Y,
                Z = x.Z
            })
            .FirstOrDefaultAsync(cancellationToken) ?? new CharacterPositionDto { X = 0, Y = 0, Z = 0 };
    }
}
