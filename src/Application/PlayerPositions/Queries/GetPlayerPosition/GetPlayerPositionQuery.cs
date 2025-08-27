using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.Common.Attributes;

namespace ProjectX.Application.PlayerPositions.Queries.GetPlayerPosition;

public record class GetPlayerPositionQuery(int PlayerId) : IRequest<PlayerPositionDto>;

public class GetPlayerPositionQueryHandler : IRequestHandler<GetPlayerPositionQuery, PlayerPositionDto>
{
    private readonly IApplicationDbContext _context;

    public GetPlayerPositionQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PlayerPositionDto> Handle(GetPlayerPositionQuery request, CancellationToken cancellationToken)
    {
        return await _context.PlayerPositions
            .Where(x => x.PlayerId == request.PlayerId)
            .OrderByDescending(x => x.ModDate)
            .Select(x => new PlayerPositionDto
            {
                X = x.X,
                Y = x.Y,
                Z = x.Z
            })
            .FirstOrDefaultAsync(cancellationToken) ?? new PlayerPositionDto { X = 0, Y = 0, Z = 0 };
    }
}
