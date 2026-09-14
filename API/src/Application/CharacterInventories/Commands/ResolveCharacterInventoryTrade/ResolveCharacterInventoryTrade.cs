using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.CharacterInventories.Commands.TradeCharacterInventories;
using ProjectX.Application.CharacterInventories.Queries.GetCharacterInventory;
using ProjectX.Application.Common.Extensions;
using ProjectX.Application.Common.Interfaces;

namespace ProjectX.Application.CharacterInventories.Commands.ResolveCharacterInventoryTrade;

public record ResolveCharacterInventoryTradeCommand(Guid TradeId) : IRequest<TradeCharacterInventoriesDto>
{
    public override string ToString()
    {
        return $"{nameof(ResolveCharacterInventoryTradeCommand)} {{ TradeId = {TradeId} }}";
    }
}

public class ResolveCharacterInventoryTradeCommandHandler
    : IRequestHandler<ResolveCharacterInventoryTradeCommand, TradeCharacterInventoriesDto>
{
    private readonly IApplicationDbContext _context;

    public ResolveCharacterInventoryTradeCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TradeCharacterInventoriesDto> Handle(
        ResolveCharacterInventoryTradeCommand request,
        CancellationToken cancellationToken)
    {
        var receipt = await _context.CharacterInventoryTradeReceipts
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == request.TradeId, cancellationToken);

        if (receipt is null)
        {
            return new TradeCharacterInventoriesDto
            {
                Status = TradeCharacterInventoriesStatusEnum.ReceiptNotFound
            };
        }

        var sourceInventory = await GetInventorySnapshotAsync(receipt.SourceCharacterId, cancellationToken);
        var targetInventory = await GetInventorySnapshotAsync(receipt.TargetCharacterId, cancellationToken);

        return new TradeCharacterInventoriesDto
        {
            Status = TradeCharacterInventoriesStatusEnum.Applied,
            SourceInventory = sourceInventory,
            TargetInventory = targetInventory
        };
    }

    private async Task<CharacterInventoryDto> GetInventorySnapshotAsync(
        int characterId,
        CancellationToken cancellationToken)
    {
        var entity = await _context.CharacterInventories
            .AsNoTracking()
            .Where(x => x.Id == characterId)
            .SingleOrNotFoundAsync("character inventory", cancellationToken);
        var effectiveCapacity = Math.Max(entity.Count, entity.Inventory.Items.Count);

        if (effectiveCapacity > short.MaxValue)
        {
            throw new InvalidOperationException("The character inventory exceeds the supported capacity.");
        }

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
