using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.Characters.Commands;

public class UpdateCharacterCommand : IRequest
{
    public int CharacterId { get; init; }

    public int? Health { get; init; }

    public short? Strength { get; init; }

    public short? Agility { get; init; }

    public short? Stamina { get; init; }

    public short? Intelligence { get; init; }

    public short? Spirit { get; init; }

    public short? Arrmor { get; init; }

    public InventoryItemEnum? Helmet { get; init; }

    public InventoryItemEnum? Chest { get; init; }

    public InventoryItemEnum? Boots { get; init; }

    public InventoryItemEnum? Weapon { get; init; }
}

public class UpdateCharacterCommandHandler : IRequestHandler<UpdateCharacterCommand>
{
    private static readonly Serilog.ILogger Log = Serilog.Log.ForContext<UpdateCharacterCommandHandler>();

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
            //.Where(x => x.Id == request.CharacterId)
            .Where(x => x.ApplicationUserId == userId)
            .SingleAsync(cancellationToken);

        Log.Debug("Found character. CharacterId {0}, UserId: {1}", character.Id, userId);

        if (request.Health is not null)
        {
            Log.Debug("Update {0} from {1} to {2}", nameof(character.Health), character.Health, request.Health.Value);

            character.Health = request.Health.Value;
        }

        if (request.Strength is not null)
        {
            Log.Debug("Update {0} from {1} to {2}", nameof(character.Strength), character.Strength, request.Strength.Value);
            character.Strength = request.Strength.Value;
        }

        if (request.Agility is not null)
        {
            Log.Debug("Update {0} from {1} to {2}", nameof(character.Agility), character.Agility, request.Agility.Value);

            character.Agility = request.Agility.Value;
        }

        if (request.Stamina is not null)
        {
            Log.Debug("Update {0} from {1} to {2}", nameof(character.Stamina), character.Stamina, request.Stamina.Value);

            character.Stamina = request.Stamina.Value;
        }

        if (request.Intelligence is not null)
        {
            Log.Debug("Update {0} from {1} to {2}", nameof(character.Intelligence), character.Intelligence, request.Intelligence.Value);

            character.Intelligence = request.Intelligence.Value;
        }

        if (request.Spirit is not null)
        {
            Log.Debug("Update {0} from {1} to {2}", nameof(character.Spirit), character.Spirit, request.Spirit.Value);

            character.Spirit = request.Spirit.Value;
        }

        if (request.Arrmor is not null)
        {
            Log.Debug("Update {0} from {1} to {2}", nameof(character.Arrmor), character.Arrmor, request.Arrmor.Value);

            character.Arrmor = request.Arrmor.Value;
        }

        if (request.Helmet is not null)
        {
            Log.Debug("Update {0} from {1} to {2}", nameof(character.Helmet), character.Helmet, request.Helmet.Value);

            character.Helmet = request.Helmet.Value;
        }

        if (request.Chest is not null)
        {
            Log.Debug("Update {0} from {1} to {2}", nameof(character.Chest), character.Chest, request.Chest.Value);

            character.Chest = request.Chest.Value;
        }

        if (request.Boots is not null)
        {
            Log.Debug("Update {0} from {1} to {2}", nameof(character.Boots), character.Boots, request.Boots.Value);

            character.Boots = request.Boots.Value;
        }

        if (request.Weapon is not null)
        {
            Log.Debug("Update {0} from {1} to {2}", nameof(character.Weapon), character.Weapon, request.Weapon.Value);

            character.Weapon = request.Weapon.Value;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
