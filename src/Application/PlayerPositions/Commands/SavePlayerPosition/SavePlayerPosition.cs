using Application.Common.Interfaces;
using MediatR;
using ProjectX.Application.Common.Attributes;
using ProjectX.Domain.Entities;

namespace ProjectX.Application.PlayerPositions.Commands.SavePlayerPosition;

[Authorize]
public record SavePlayerPositionCommand : IRequest
{
    public int PlayerId { get; set; }

    public float X { get; set; }

    public float Y { get; set; }

    public float Z { get; set; }
}

public class SavePlayerPositionCommandHandler : IRequestHandler<SavePlayerPositionCommand>
{
    private readonly IApplicationDbContext _context;

    public SavePlayerPositionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(SavePlayerPositionCommand request, CancellationToken cancellationToken)
    {
        var entity = new PlayerPosition
        {
            PlayerId = request.PlayerId,
            X = request.X,
            Y = request.Y,
            Z = request.Z,
            ModDate = DateTime.Now
        };

        _context.PlayerPositions.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
