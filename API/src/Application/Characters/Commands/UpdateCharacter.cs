using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.Characters.Commands;

public class UpdateCharacterCommand : IRequest
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

        if (request.MaxHealth is not null)
        {
            Log.Debug("Update {0} from {1} to {2}", nameof(character.MaxHealth), character.MaxHealth, request.MaxHealth.Value);

            character.MaxHealth = request.MaxHealth.Value;
        }

        if (request.Strength is not null)
        {
            Log.Debug("Update {0} from {1} to {2}", nameof(character.Strength), character.Strength, request.Strength.Value);
            character.Strength = request.Strength.Value;
        }

        if (request.Dexterity is not null)
        {
            Log.Debug("Update {0} from {1} to {2}", nameof(character.Dexterity), character.Dexterity, request.Dexterity.Value);

            character.Dexterity = request.Dexterity.Value;
        }

        if (request.Speed is not null)
        {
            Log.Debug("Update {0} from {1} to {2}", nameof(character.Speed), character.Speed, request.Speed.Value);

            character.Speed = request.Speed.Value;
        }

        if (request.Intellect is not null)
        {
            Log.Debug("Update {0} from {1} to {2}", nameof(character.Intellect), character.Intellect, request.Intellect.Value);

            character.Intellect = request.Intellect.Value;
        }

        if (request.Armor is not null)
        {
            Log.Debug("Update {0} from {1} to {2}", nameof(character.Armor), character.Armor, request.Armor.Value);

            character.Armor = request.Armor.Value;
        }

        if (request.HelmetType is not null)
        {
            Log.Debug("Update {0} from {1} to {2}", nameof(character.HelmetType), character.HelmetType, request.HelmetType.Value);

            character.HelmetType = request.HelmetType.Value;
        }

        if (request.ChestType is not null)
        {
            Log.Debug("Update {0} from {1} to {2}", nameof(character.ChestType), character.ChestType, request.ChestType.Value);

            character.ChestType = request.ChestType.Value;
        }

        if (request.BootsType is not null)
        {
            Log.Debug("Update {0} from {1} to {2}", nameof(character.BootsType), character.BootsType, request.BootsType.Value);

            character.BootsType = request.BootsType.Value;
        }

        if (request.WeaponType is not null)
        {
            Log.Debug("Update {0} from {1} to {2}", nameof(character.WeaponType), character.WeaponType, request.WeaponType.Value);

            character.WeaponType = request.WeaponType.Value;
        }

        if (request.AmmoType is not null)
        {
            Log.Debug("Update {0} from {1} to {2}", nameof(character.AmmoType), character.AmmoType, request.AmmoType.Value);

            character.AmmoType = request.AmmoType.Value;
        }

        if (request.AmmoCount is not null)
        {
            Log.Debug("Update {0} from {1} to {2}", nameof(character.AmmoCount), character.AmmoCount, request.AmmoCount.Value);

            character.AmmoCount = request.AmmoCount.Value;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
