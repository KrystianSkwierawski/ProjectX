using Application.Common.Interfaces;
using MediatR;
using ProjectX.Application.Common.Attributes;
using ProjectX.Domain.Entities;

namespace ProjectX.Application.PlayerPositions.Commands.SavePlayerPosition;

[Authorize]
public record SaveCharacterPositionCommand : IRequest
{
    public int PlayerId { get; set; }

    public float X { get; set; }

    public float Y { get; set; }

    public float Z { get; set; }
}

public class SavePlayerPositionCommandHandler : IRequestHandler<SaveCharacterPositionCommand>
{
    private readonly IApplicationDbContext _context;

    public SavePlayerPositionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(SaveCharacterPositionCommand request, CancellationToken cancellationToken)
    {
        var entity = new CharacterPosition
        {
            CharacterId = request.PlayerId,
            X = request.X,
            Y = request.Y,
            Z = request.Z,
            ModDate = DateTime.Now
        };

        _context.CharacterPosition.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
