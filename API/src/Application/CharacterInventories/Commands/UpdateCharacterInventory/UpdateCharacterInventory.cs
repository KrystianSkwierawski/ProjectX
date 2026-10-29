using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.CharacterInventories.Queries.GetCharacterInventory;
using ProjectX.Application.Characters.Commands;
using ProjectX.Application.Common.Extensions;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Entities;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.CharacterInventories.Commands.UpdateCharacterInventory;

public record UpdateCharacterInventoryCommand(
    InventoryItemDto[] Add,
    InventoryItemDto[] Remove,
    int? SplitSlotIndex = null,
    int? MoveSourceSlotIndex = null,
    int? MoveTargetSlotIndex = null,
    UpdateCharacterCommand? CharacterUpdate = null,
    Guid? HealthOperationId = null) : IRequest<UpdateCharacterInventoryDto>;

public class UpdateCharacterInventoryCommandHandler : IRequestHandler<UpdateCharacterInventoryCommand, UpdateCharacterInventoryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateCharacterInventoryCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<UpdateCharacterInventoryDto> Handle(UpdateCharacterInventoryCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetId();
        var selectedCharacterId = _currentUserService.GetRequiredCharacterId();

        var fingerprint = request.HealthOperationId.HasValue
            ? CreateRequestFingerprint(request)
            : null;

        if (request.HealthOperationId.HasValue)
        {
            var hasHealthUpdate = request.CharacterUpdate?.Health.HasValue == true;
            var hasAmmoUpdate = request.CharacterUpdate?.AmmoType.HasValue == true
                && request.CharacterUpdate.AmmoCount.HasValue
                && request.Add.Length == 0 && request.Remove.Length == 0;
            var hasLayoutFields = request.SplitSlotIndex.HasValue
                || request.MoveSourceSlotIndex.HasValue || request.MoveTargetSlotIndex.HasValue;
            var hasLayoutUpdate = request.CharacterUpdate == null
                && request.Add.Length == 0 && request.Remove.Length == 0
                && ((request.SplitSlotIndex.HasValue && !request.MoveSourceSlotIndex.HasValue && !request.MoveTargetSlotIndex.HasValue)
                    || (!request.SplitSlotIndex.HasValue && request.MoveSourceSlotIndex.HasValue && request.MoveTargetSlotIndex.HasValue));

            if (request.HealthOperationId == Guid.Empty
                || (hasLayoutFields ? !hasLayoutUpdate : !hasHealthUpdate && !hasAmmoUpdate))
            {
                throw new InvalidOperationException("Inventory operation receipts require a health/ammo transaction or a standalone layout change.");
            }

            var previous = await ReadReceiptAsync(request, selectedCharacterId, fingerprint!, cancellationToken);

            if (previous != null)
            {
                return previous;
            }
        }

        var entity = await _context.CharacterInventories
            .Include(x => x.Character)
            .Where(x => x.Id == selectedCharacterId)
            .Where(x => x.Character.ApplicationUserId == userId)
            .SingleOrNotFoundAsync("character inventory", cancellationToken);

        var inventory = entity.Inventory.Clone();
        var effectiveCapacity = Math.Max(entity.Count, inventory.Items.Count);

        if (effectiveCapacity > short.MaxValue)
        {
            throw new InvalidOperationException("The character inventory exceeds the supported capacity.");
        }

        if (request.SplitSlotIndex.HasValue)
        {
            if (!inventory.Split(request.SplitSlotIndex.Value, effectiveCapacity))
            {
                if (request.HealthOperationId.HasValue)
                {
                    return await SaveRejectedReceiptAsync(request, selectedCharacterId, fingerprint!, cancellationToken);
                }

                EnsureApplied(false, "split inventory stack");
            }
        }

        if (request.MoveSourceSlotIndex.HasValue && request.MoveTargetSlotIndex.HasValue)
        {
            if (!inventory.Move(request.MoveSourceSlotIndex.Value, request.MoveTargetSlotIndex.Value, effectiveCapacity))
            {
                if (request.HealthOperationId.HasValue)
                {
                    return await SaveRejectedReceiptAsync(request, selectedCharacterId, fingerprint!, cancellationToken);
                }

                EnsureApplied(false, "move inventory item");
            }
        }

        foreach (var item in request.Remove)
        {
            EnsureValidItem(item, "remove inventory item");
            if (!inventory.Remove(item.Type, item.Count))
            {
                if (request.HealthOperationId.HasValue)
                {
                    return await SaveRejectedReceiptAsync(request, selectedCharacterId, fingerprint!, cancellationToken);
                }

                EnsureApplied(false, "remove inventory item");
            }
        }

        foreach (var item in request.Add)
        {
            EnsureValidItem(item, "add inventory item");

            if (!inventory.Add(item.Type, item.Count, effectiveCapacity))
            {
                if (request.HealthOperationId.HasValue)
                {
                    return await SaveRejectedReceiptAsync(request, selectedCharacterId, fingerprint!, cancellationToken);
                }

                return new UpdateCharacterInventoryDto
                {
                    Status = UpdateCharacterInventoryStatusEnum.InventoryFull
                };
            }
        }

        entity.Inventory = inventory;
        entity.Count = (short)effectiveCapacity;

        if (request.CharacterUpdate != null)
        {
            entity.Character.UpdateState(request.CharacterUpdate.ToStateUpdate());
        }

        var characterQuests = await _context.CharacterQuests
            .Include(x => x.Quest)
            .Where(x => x.CharacterId == selectedCharacterId)
            .Where(x => x.Status == CharacterQuestStatusEnum.Accepted || x.Status == CharacterQuestStatusEnum.Finished)
            .ToArrayAsync(cancellationToken);

        foreach (var characterQuest in characterQuests)
        {

            characterQuest.SynchronizeInventory(inventory);
        }

        // Persist Collect progress with inventory even when the Unity player has despawned
        // and can no longer deliver its quest subscription notification.
        if (request.HealthOperationId.HasValue)
        {
            _context.CharacterInventoryHealthReceipts.Add(new CharacterInventoryHealthReceipt
            {
                Id = request.HealthOperationId.Value,
                CharacterId = selectedCharacterId,
                RequestFingerprint = fingerprint!,
                Applied = true
            });
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException) when (request.HealthOperationId.HasValue)
        {
            // An overlapping retry may have committed first. Otherwise retain uncertainty;
            // the caller must retry the same identifier rather than release the HP queue.
            var previous = await ReadReceiptAsync(request, selectedCharacterId, fingerprint!, cancellationToken);

            if (previous != null)
            {
                return previous;
            }

            throw;
        }
        catch (DbUpdateConcurrencyException)
        {
            return new UpdateCharacterInventoryDto
            {
                Status = UpdateCharacterInventoryStatusEnum.InventoryChanged
            };
        }

        return new UpdateCharacterInventoryDto
        {
            Status = UpdateCharacterInventoryStatusEnum.Applied
        };
    }

    private static string CreateRequestFingerprint(UpdateCharacterInventoryCommand request)
    {
        if (request.SplitSlotIndex.HasValue || request.MoveSourceSlotIndex.HasValue || request.MoveTargetSlotIndex.HasValue)
        {
            var layoutPayload = FormattableString.Invariant(
                $"layout|{request.SplitSlotIndex}|{request.MoveSourceSlotIndex}|{request.MoveTargetSlotIndex}");

            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(layoutPayload)));
        }

        // Preserve existing potion fingerprints; gear/ammo receipts bind every stat/equipment
        // field and the complete ordered inventory payload, including ammo returns.
        if (request.CharacterUpdate == new UpdateCharacterCommand { Health = request.CharacterUpdate?.Health }
            && request.Add.Length == 0 && request.Remove.Length == 1
            && request.Remove[0].Type == InventoryItemEnum.HealthPotion && request.Remove[0].Count == 1)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
                request.CharacterUpdate.Health?.ToString(CultureInfo.InvariantCulture) ?? string.Empty)));
        }

        var state = request.CharacterUpdate;
        var payload = new StringBuilder(FormattableString.Invariant(
            $"{state?.Health}|{state?.MaxHealth}|{state?.Strength}|{state?.Dexterity}|{state?.Speed}|{state?.Intellect}|{state?.Armor}|{state?.HelmetType}|{state?.ChestType}|{state?.BootsType}|{state?.WeaponType}|{state?.AmmoType}|{state?.AmmoCount}|"));

        foreach (var item in request.Add)
        {
            payload.Append(FormattableString.Invariant($"{(int)item.Type}:{item.Count},"));
        }

        payload.Append('|');

        foreach (var item in request.Remove)
        {
            payload.Append(FormattableString.Invariant($"{(int)item.Type}:{item.Count},"));
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload.ToString())));
    }

    private async Task<UpdateCharacterInventoryDto?> ReadReceiptAsync(UpdateCharacterInventoryCommand request,
        int characterId, string fingerprint, CancellationToken cancellationToken)
    {
        var receipt = await _context.CharacterInventoryHealthReceipts
            .AsNoTracking()
            .Where(x => x.Id == request.HealthOperationId)
            .SingleOrDefaultAsync(cancellationToken);

        if (receipt == null)
        {
            return null;
        }

        if (receipt.CharacterId != characterId || receipt.RequestFingerprint != fingerprint)
        {
            throw new InvalidOperationException("An inventory operation identifier cannot be reused with another character or payload.");
        }

        return new UpdateCharacterInventoryDto
        {
            Status = receipt.Applied ? UpdateCharacterInventoryStatusEnum.Applied : UpdateCharacterInventoryStatusEnum.OperationRejected
        };
    }

    private async Task<UpdateCharacterInventoryDto> SaveRejectedReceiptAsync(UpdateCharacterInventoryCommand request,
        int characterId, string fingerprint, CancellationToken cancellationToken)
    {
        _context.CharacterInventoryHealthReceipts.Add(new CharacterInventoryHealthReceipt
        {
            Id = request.HealthOperationId!.Value,
            CharacterId = characterId,
            RequestFingerprint = fingerprint,
            Applied = false
        });

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            var previous = await ReadReceiptAsync(request, characterId, fingerprint, cancellationToken);

            if (previous != null)
            {
                return previous;
            }

            throw;
        }

        return new UpdateCharacterInventoryDto { Status = UpdateCharacterInventoryStatusEnum.OperationRejected };
    }

    private static void EnsureValidItem(InventoryItemDto item, string operation)
    {
        if (item.Type == InventoryItemEnum.None || item.Count <= 0)
        {
            throw new InvalidOperationException($"The game server requested an invalid operation: {operation}.");
        }
    }

    private static void EnsureApplied(bool applied, string operation)
    {
        if (!applied)
        {
            throw new InvalidOperationException($"The game server requested an invalid operation: {operation}.");
        }
    }
}
