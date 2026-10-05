using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.CharacterInventories.Queries.GetCharacterInventory;
using ProjectX.Application.Common.Extensions;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Entities;
using ProjectX.Domain.Enums;
using ProjectX.Domain.Inventory;

namespace ProjectX.Application.CharacterStashes;


public record AccessCharacterStashCommand(
    StashOperationEnum Operation,
    long Revision = 0,
    int SourceIndex = -1,
    int? TargetIndex = null,
    InventoryItemEnum ExpectedType = InventoryItemEnum.None,
    int ExpectedCount = 0) : IRequest<CharacterStashDto>
{
    public override string ToString() => $"Stash {{ Operation = {Operation}, Revision = {Revision}, SourceIndex = {SourceIndex} }}";
}

public class AccessCharacterStashCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<AccessCharacterStashCommand, CharacterStashDto>
{
    public async Task<CharacterStashDto> Handle(AccessCharacterStashCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.GetId();
        var characterId = currentUser.GetRequiredCharacterId();
        var characterInventory = await context.CharacterInventories
            .Where(x => x.Id == characterId)
            .Where(x => x.Character.ApplicationUserId == userId)
            .SingleOrNotFoundAsync("character inventory", cancellationToken);

        var stash = await context.CharacterStashes.Where(x => x.ApplicationUserId == userId)
            .SingleOrDefaultAsync(cancellationToken);
        var isNew = stash == null;
        stash ??= new CharacterStash { ApplicationUserId = userId };

        if (request.Operation == StashOperationEnum.Read)
        {
            return Snapshot(stash, characterInventory);
        }

        if (request.Revision != stash.Revision)
        {
            return new() { Status = StashStatusEnum.Changed };
        }

        var inventory = characterInventory.Inventory.Clone();
        var storage = stash.Inventory.Clone();
        var source = request.Operation == StashOperationEnum.Deposit ? inventory : storage;

        if (request.SourceIndex < 0 || request.SourceIndex >= source.Items.Count
            || source.Items[request.SourceIndex].IsEmpty
            || source.Items[request.SourceIndex].Type != request.ExpectedType
            || source.Items[request.SourceIndex].Count != request.ExpectedCount)
        {
            return new() { Status = StashStatusEnum.Changed };
        }

        var capacity = Math.Max(characterInventory.Count, inventory.Items.Count);
        var applied = request.Operation switch
        {
            StashOperationEnum.Deposit => inventory.TransferTo(storage, request.SourceIndex, stash.Count, request.TargetIndex),
            StashOperationEnum.Withdraw => storage.TransferTo(inventory, request.SourceIndex, capacity, request.TargetIndex),
            StashOperationEnum.Split => storage.Split(request.SourceIndex, stash.Count),
            StashOperationEnum.Move when request.TargetIndex.HasValue => storage.Move(request.SourceIndex, request.TargetIndex.Value, stash.Count),
            _ => false
        };

        if (!applied)
        {
            return new() { Status = StashStatusEnum.Full };
        }

        var quests = await context.CharacterQuests.Include(x => x.Quest)
            .Where(x => x.CharacterId == characterId)
            .Where(x => x.Quest.Type == QuestTypeEnum.Collect)
            .Where(x => x.Status == CharacterQuestStatusEnum.Accepted || x.Status == CharacterQuestStatusEnum.Finished)
            .ToArrayAsync(cancellationToken);

        foreach (var quest in quests)
        {
            quest.SetProgress(inventory.GetCount(Enum.Parse<InventoryItemEnum>(quest.Quest.GameObjectName)), quest.Quest.Requirement);
        }

        characterInventory.Inventory = inventory;
        stash.Inventory = storage;
        stash.Revision++;

        if (isNew)
        {
            context.CharacterStashes.Add(stash);
        }

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new() { Status = StashStatusEnum.Changed };
        }
        catch (DbUpdateException) when (isNew)
        {
            // Concurrent first access from another character on this account.
            if (await context.CharacterStashes.AsNoTracking().Where(x => x.ApplicationUserId == userId).AnyAsync(cancellationToken))
            {
                return new() { Status = StashStatusEnum.Changed };
            }

            throw;
        }

        return Snapshot(stash, characterInventory);
    }

    private static CharacterStashDto Snapshot(CharacterStash stash, CharacterInventory inventory) => new()
    {
        Revision = stash.Revision,
        Count = stash.Count,
        Inventory = ToDto(stash.Inventory),
        CharacterInventory = new CharacterInventoryDto
        {
            CharacterId = inventory.Id,
            Count = (short)Math.Max(inventory.Count, inventory.Inventory.Items.Count),
            Inventory = ToDto(inventory.Inventory)
        }
    };

    private static InventoryDto ToDto(InventoryState state) => new()
    {
        Items = state.Items.Select(x => new InventoryItemDto { Type = x.Type, Count = x.Count }).ToArray()
    };
}
