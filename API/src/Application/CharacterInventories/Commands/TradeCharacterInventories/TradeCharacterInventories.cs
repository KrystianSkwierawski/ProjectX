using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.CharacterInventories.Queries.GetCharacterInventory;
using ProjectX.Application.Common.Exceptions;
using ProjectX.Application.Common.Extensions;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Entities;
using ProjectX.Domain.Enums;
using ProjectX.Domain.Inventory;

namespace ProjectX.Application.CharacterInventories.Commands.TradeCharacterInventories;

public record TradeCharacterInventoriesCommand(
    Guid TradeId,
    string OtherPlayerSessionId,
    InventoryItemDto[] Give,
    InventoryItemDto[] Receive) : IRequest<TradeCharacterInventoriesDto>
{
    public override string ToString()
    {
        return $"{nameof(TradeCharacterInventoriesCommand)} {{ TradeId = {TradeId}, GiveItemTypes = {Give?.Length ?? 0}, ReceiveItemTypes = {Receive?.Length ?? 0} }}";
    }
}

public class TradeCharacterInventoriesCommandHandler : IRequestHandler<TradeCharacterInventoriesCommand, TradeCharacterInventoriesDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IGameSessionService _gameSessionService;

    public TradeCharacterInventoriesCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IGameSessionService gameSessionService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _gameSessionService = gameSessionService;
    }

    public async Task<TradeCharacterInventoriesDto> Handle(
        TradeCharacterInventoriesCommand request,
        CancellationToken cancellationToken)
    {
        var sourceUserId = _currentUserService.GetId();
        var sourceCharacterId = _currentUserService.GetRequiredCharacterId();
        var serverUserId = _currentUserService.GetAuthenticatedUserId();

        if (!_gameSessionService.TryResolvePlayer(serverUserId, request.OtherPlayerSessionId, out var targetPlayer))
        {
            throw new InvalidGameSessionCredentialException();
        }

        if (sourceCharacterId == targetPlayer.CharacterId)
        {
            throw new InvalidOperationException("The game server requested a trade with the same character on both sides.");
        }

        var requestFingerprint = CreateRequestFingerprint(sourceCharacterId, targetPlayer.CharacterId, request);

        if (await WasAlreadyAppliedAsync(
            request.TradeId,
            sourceCharacterId,
            targetPlayer.CharacterId,
            requestFingerprint,
            cancellationToken))
        {
            return await GetAppliedResultAsync(
                sourceCharacterId,
                sourceUserId,
                targetPlayer.CharacterId,
                targetPlayer.UserId,
                cancellationToken);
        }

        var sourceEntity = await GetInventoryAsync(sourceCharacterId, sourceUserId, cancellationToken);
        var targetEntity = await GetInventoryAsync(targetPlayer.CharacterId, targetPlayer.UserId, cancellationToken);
        var sourceInventory = sourceEntity.Inventory.Clone();
        var targetInventory = targetEntity.Inventory.Clone();
        var sourceCapacity = GetEffectiveCapacity(sourceEntity, sourceInventory);
        var targetCapacity = GetEffectiveCapacity(targetEntity, targetInventory);

        if (!TryRemove(sourceInventory, request.Give))
        {
            return Result(TradeCharacterInventoriesStatusEnum.SourceItemsUnavailable);
        }

        if (!TryRemove(targetInventory, request.Receive))
        {
            return Result(TradeCharacterInventoriesStatusEnum.TargetItemsUnavailable);
        }

        if (!TryAdd(sourceInventory, request.Receive, sourceCapacity))
        {
            return Result(TradeCharacterInventoriesStatusEnum.SourceInventoryFull);
        }

        if (!TryAdd(targetInventory, request.Give, targetCapacity))
        {
            return Result(TradeCharacterInventoriesStatusEnum.TargetInventoryFull);
        }

        await SynchronizeCollectQuestProgressAsync(
            sourceCharacterId,
            sourceInventory,
            targetPlayer.CharacterId,
            targetInventory,
            cancellationToken);

        sourceEntity.Inventory = sourceInventory;
        sourceEntity.Count = (short)sourceCapacity;
        targetEntity.Inventory = targetInventory;
        targetEntity.Count = (short)targetCapacity;

        _context.CharacterInventoryTradeReceipts.Add(CharacterInventoryTradeReceipt.Create(
            request.TradeId,
            sourceCharacterId,
            targetPlayer.CharacterId,
            requestFingerprint));

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (await WasAlreadyAppliedAsync(
                request.TradeId,
                sourceCharacterId,
                targetPlayer.CharacterId,
                requestFingerprint,
                cancellationToken))
            {
                return await GetAppliedResultAsync(
                    sourceCharacterId,
                    sourceUserId,
                    targetPlayer.CharacterId,
                    targetPlayer.UserId,
                    cancellationToken);
            }

            return Result(TradeCharacterInventoriesStatusEnum.InventoryChanged);
        }
        catch (DbUpdateException)
        {
            if (await WasAlreadyAppliedAsync(
                request.TradeId,
                sourceCharacterId,
                targetPlayer.CharacterId,
                requestFingerprint,
                cancellationToken))
            {
                return await GetAppliedResultAsync(
                    sourceCharacterId,
                    sourceUserId,
                    targetPlayer.CharacterId,
                    targetPlayer.UserId,
                    cancellationToken);
            }

            throw;
        }

        return AppliedResult(sourceEntity, targetEntity);
    }

    private async Task<CharacterInventory> GetInventoryAsync(int characterId, string userId, CancellationToken cancellationToken)
    {
        return await _context.CharacterInventories
            .Where(x => x.Id == characterId)
            .Where(x => x.Character.ApplicationUserId == userId)
            .SingleOrNotFoundAsync("character inventory", cancellationToken);
    }

    private async Task<TradeCharacterInventoriesDto> GetAppliedResultAsync(
        int sourceCharacterId,
        string sourceUserId,
        int targetCharacterId,
        string targetUserId,
        CancellationToken cancellationToken)
    {
        var sourceInventory = await GetInventorySnapshotAsync(sourceCharacterId, sourceUserId, cancellationToken);
        var targetInventory = await GetInventorySnapshotAsync(targetCharacterId, targetUserId, cancellationToken);

        return new TradeCharacterInventoriesDto
        {
            Status = TradeCharacterInventoriesStatusEnum.Applied,
            SourceInventory = sourceInventory,
            TargetInventory = targetInventory
        };
    }

    private async Task<CharacterInventoryDto> GetInventorySnapshotAsync(
        int characterId,
        string userId,
        CancellationToken cancellationToken)
    {
        var entity = await _context.CharacterInventories
            .AsNoTracking()
            .Where(x => x.Id == characterId)
            .Where(x => x.Character.ApplicationUserId == userId)
            .SingleOrNotFoundAsync("character inventory", cancellationToken);

        return CreateInventorySnapshot(entity);
    }

    private static int GetEffectiveCapacity(CharacterInventory entity, InventoryState inventory)
    {
        var effectiveCapacity = Math.Max(entity.Count, inventory.Items.Count);

        if (effectiveCapacity > short.MaxValue)
        {
            throw new InvalidOperationException("The character inventory exceeds the supported capacity.");
        }

        return effectiveCapacity;
    }

    private static bool TryRemove(InventoryState inventory, IEnumerable<InventoryItemDto> items)
    {
        foreach (var item in items)
        {
            EnsureValidItem(item);

            if (!inventory.Remove(item.Type, item.Count))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryAdd(InventoryState inventory, IEnumerable<InventoryItemDto> items, int capacity)
    {
        foreach (var item in items)
        {
            EnsureValidItem(item);

            if (!inventory.Add(item.Type, item.Count, capacity))
            {
                return false;
            }
        }

        return true;
    }

    private async Task SynchronizeCollectQuestProgressAsync(
        int sourceCharacterId,
        InventoryState sourceInventory,
        int targetCharacterId,
        InventoryState targetInventory,
        CancellationToken cancellationToken)
    {
        var characterQuests = await _context.CharacterQuests
            .Include(x => x.Quest)
            .Where(x => x.CharacterId == sourceCharacterId || x.CharacterId == targetCharacterId)
            .Where(x => x.Quest.Type == QuestTypeEnum.Collect)
            .Where(x => x.Status == CharacterQuestStatusEnum.Accepted || x.Status == CharacterQuestStatusEnum.Finished)
            .ToArrayAsync(cancellationToken);

        foreach (var characterQuest in characterQuests)
        {
            var itemType = Enum.Parse<InventoryItemEnum>(characterQuest.Quest.GameObjectName);
            var inventory = characterQuest.CharacterId == sourceCharacterId
                ? sourceInventory
                : targetInventory;

            characterQuest.SetProgress(
                inventory.GetCount(itemType),
                characterQuest.Quest.Requirement);
        }
    }

    private static void EnsureValidItem(InventoryItemDto item)
    {
        if (!Enum.IsDefined(item.Type) || item.Type == InventoryItemEnum.None || item.Count <= 0)
        {
            throw new InvalidOperationException("The game server requested an invalid trade item.");
        }
    }

    private async Task<bool> WasAlreadyAppliedAsync(
        Guid tradeId,
        int sourceCharacterId,
        int targetCharacterId,
        string requestFingerprint,
        CancellationToken cancellationToken)
    {
        var receipt = await _context.CharacterInventoryTradeReceipts
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == tradeId, cancellationToken);

        if (receipt is null)
        {
            return false;
        }

        if (!receipt.Matches(sourceCharacterId, targetCharacterId, requestFingerprint))
        {
            throw new InvalidOperationException("A trade identifier was reused with a different request.");
        }

        return true;
    }

    private static string CreateRequestFingerprint(
        int sourceCharacterId,
        int targetCharacterId,
        TradeCharacterInventoriesCommand request)
    {
        var payload = new StringBuilder()
            .Append(sourceCharacterId)
            .Append('|')
            .Append(targetCharacterId)
            .Append('|');

        AppendItems(payload, request.Give);
        payload.Append('|');
        AppendItems(payload, request.Receive);

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload.ToString())));
    }

    private static void AppendItems(StringBuilder payload, IEnumerable<InventoryItemDto> items)
    {
        foreach (var item in items)
        {
            payload
                .Append((int)item.Type)
                .Append(':')
                .Append(item.Count)
                .Append(',');
        }
    }

    private static TradeCharacterInventoriesDto Result(TradeCharacterInventoriesStatusEnum status)
    {
        return new TradeCharacterInventoriesDto { Status = status };
    }

    private static TradeCharacterInventoriesDto AppliedResult(CharacterInventory source, CharacterInventory target)
    {
        return new TradeCharacterInventoriesDto
        {
            Status = TradeCharacterInventoriesStatusEnum.Applied,
            SourceInventory = CreateInventorySnapshot(source),
            TargetInventory = CreateInventorySnapshot(target)
        };
    }

    private static CharacterInventoryDto CreateInventorySnapshot(CharacterInventory entity)
    {
        var effectiveCapacity = GetEffectiveCapacity(entity, entity.Inventory);

        return new CharacterInventoryDto
        {
            CharacterId = entity.Id,
            Inventory = new InventoryDto
            {
                Items = entity.Inventory.Items
                    .Select(x => new InventoryItemDto { Type = x.Type, Count = x.Count })
                    .ToList()
            },
            Count = (short)effectiveCapacity
        };
    }
}
