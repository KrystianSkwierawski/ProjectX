using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.CharacterInventories.Queries.GetCharacterInventory;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Entities;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.CharacterQuests.Commands.CompleteCharacterQuest;

public record CompleteCharacterQuestCommand(int CharacterQuestId) : IRequest<CompleteCharacterQuestDto>;

public class CompleteCharacterQuestCommandHandler : IRequestHandler<CompleteCharacterQuestCommand, CompleteCharacterQuestDto>
{
    private static readonly Serilog.ILogger Log = Serilog.Log.ForContext<CompleteCharacterQuestCommandHandler>();

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CompleteCharacterQuestCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<CompleteCharacterQuestDto> Handle(CompleteCharacterQuestCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetId();

        var characterQuest = await _context.CharacterQuests
            .Include(x => x.Quest)
            .Where(x => x.Id == request.CharacterQuestId)
            .Where(x => x.Status == CharacterQuestStatusEnum.Finished)
            .Where(x => x.Character.ApplicationUserId == userId)
            .SingleAsync(cancellationToken);

        Log.Debug("Found character quest for id: {0}", characterQuest.Id);

        characterQuest.EndDate = DateTime.Now;
        characterQuest.ModDate = characterQuest.EndDate;
        characterQuest.Status = CharacterQuestStatusEnum.Completed;

        if (characterQuest.Quest.Type == QuestTypeEnum.Collect)
        {
            CollectItemsAsync(userId, characterQuest, cancellationToken).Wait(cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        Log.Debug("Completed character quest for id: {0}", characterQuest.Id);

        return new CompleteCharacterQuestDto
        {
            Reward = characterQuest.Quest.Reward
        };
    }

    private async Task CollectItemsAsync(string userId, CharacterQuest characterQuest, CancellationToken cancellationToken)
    {
        var itemType = Enum.Parse<InventoryItemEnum>(characterQuest.Quest.GameObjectName);

        var characterInventory = await _context.CharacterInventories
            //.Where(x => x.CharacterId == request.CharacterId)
            .Where(x => x.Character.ApplicationUserId == userId)
            .SingleAsync(cancellationToken);

        var inventory = JsonSerializer.Deserialize<InventoryDto>(characterInventory.Inventory);

        ArgumentNullException.ThrowIfNull(inventory, nameof(inventory));

        var item = inventory.Items
            .Where(x => x.Type == itemType)
            .Where(x => x.Count >= characterQuest.Quest.Requirement)
            .First();

        if (item.Count == characterQuest.Quest.Requirement)
        {
            item.Type = InventoryItemEnum.None;
            item.Count = 0;
        }
        else
        {
            item.Count -= characterQuest.Quest.Requirement;
        }

        characterInventory.Inventory = JsonSerializer.Serialize(inventory);

        Log.Debug("Collected items. DharacterInventoryId: {0}, UserId: {1}", characterInventory.Id, userId);
    }
}
