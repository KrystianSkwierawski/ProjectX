using MediatR;
using ProjectX.Application.Common.Extensions;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Characters;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.Characters.Commands;

public record UpdateCharacterCommand : IRequest
{
    public int CharacterId { get; init; }
    public int? Health { get; init; }
    public int? MaxHealth { get; init; }
    public short? Strength { get; init; }
    public short? Dexterity { get; init; }
    public short? Speed { get; init; }
    public short? Intellect { get; init; }
    public short? Armor { get; init; }
    public InventoryItemEnum? HelmetType { get; init; }
    public InventoryItemEnum? ChestType { get; init; }
    public InventoryItemEnum? BootsType { get; init; }
    public InventoryItemEnum? WeaponType { get; init; }
    public InventoryItemEnum? AmmoType { get; init; }
    public int? AmmoCount { get; init; }
}

public class UpdateCharacterCommandHandler : IRequestHandler<UpdateCharacterCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateCharacterCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task Handle(UpdateCharacterCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetId();

        var character = await _context.Characters
            .Where(x => x.Id == request.CharacterId)
            .Where(x => x.ApplicationUserId == userId)
            .SingleOrNotFoundAsync("character", cancellationToken);

        character.UpdateState(new CharacterStateUpdate(
            request.Health,
            request.MaxHealth,
            request.Strength,
            request.Dexterity,
            request.Speed,
            request.Intellect,
            request.Armor,
            request.HelmetType,
            request.ChestType,
            request.BootsType,
            request.WeaponType,
            request.AmmoType,
            request.AmmoCount));

        await _context.SaveChangesAsync(cancellationToken);
    }
}
