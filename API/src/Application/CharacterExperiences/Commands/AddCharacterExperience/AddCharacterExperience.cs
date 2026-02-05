using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.CharacterInventories.Queries.GetCharacterInventory;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Entities;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.CharacterExperiences.Commands.AddCharacterExperience;
public record AddCharacterExperienceCommand : IRequest<AddCharacterExperienceDto>
{
    public int CharacterId { get; set; }

    public int CharacterQuestId { get; set; }

    public ExperienceTypeEnum Type { get; init; }
}

public class AddCharacterExperienceCommandHandler : IRequestHandler<AddCharacterExperienceCommand, AddCharacterExperienceDto>
{
    private static readonly SortedDictionary<int, byte> _experienceToLevel = new SortedDictionary<int, byte>
    {
        { 0, 1 },
        { 100, 2 },
        { 400, 3 },
        { 4000, 4 },
        { 5000, 5 },
        { 6000, 6 },
        { 7000, 7 },
        { 8000, 8 },
        { 9000, 9 },
        { 10000, 10 }
    };

    private static readonly Serilog.ILogger Log = Serilog.Log.ForContext<AddCharacterExperienceCommandHandler>();

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public AddCharacterExperienceCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<AddCharacterExperienceDto> Handle(AddCharacterExperienceCommand request, CancellationToken cancellationToken)
    {
        var result = new AddCharacterExperienceDto();

        int amount = 50;

        var userId = _currentUserService.GetId();

        if (request.Type == ExperienceTypeEnum.Questing)
        {
            amount = await CompleteQuestAsync(request, userId, cancellationToken);
        }

        var character = await _context.Characters
                .Include(x => x.CharacterExperiences)
                //.Where(x => x.Id == request.CharacterId)
                .Where(x => x.ApplicationUserId == userId)
                .FirstAsync(cancellationToken);

        Log.Debug("Found character. CharacterId {0}, UserId: {1}", character.Id, userId);

        character.CharacterExperiences.Add(new CharacterExperience
        {
            Amount = amount,
            Type = request.Type,
            ModDate = DateTime.Now
        });

        result.Experience = character.CharacterExperiences
            .Select(x => x.Amount)
            .Sum();

        var newLevel = _experienceToLevel
            .Where(x => x.Key <= result.Experience)
            .Max(x => x.Value);

        if (character.Level < newLevel)
        {
            var diff = (byte)(newLevel - character.Level);
            character.Level = newLevel;
            character.SkillPoints += diff;

            result.LeveledUp = true;

            Log.Debug("LeveledUp. CharacterId: {0}, LevelDiff: {1}", character.Id, diff);
        }

        await _context.SaveChangesAsync(cancellationToken);

        result.Level = character.Level;
        result.SkillPoints = character.SkillPoints;

        return result;
    }

    private async Task<int> CompleteQuestAsync(AddCharacterExperienceCommand request, string userId, CancellationToken cancellationToken)
    {
        var characterQuest = await _context.CharacterQuests
                        .Include(x => x.Quest)
                        .Where(x => x.Id == request.CharacterQuestId)
                        //.Where(x => x.CharacterId == request.CharacterId)
                        .Where(x => x.Status == CharacterQuestStatusEnum.Finished)
                        .Where(x => x.Character.ApplicationUserId == userId)
                        .SingleAsync(cancellationToken);

        var now = DateTime.Now;

        characterQuest.EndDate = now;
        characterQuest.ModDate = now;
        characterQuest.Status = CharacterQuestStatusEnum.Completed;

        Log.Debug("Completed character quest. CharacterQuestId: {0}, UserId: {1}", characterQuest.Id, userId);

        if (characterQuest.Quest.Type == QuestTypeEnum.Collect)
        {
            await CollectItemsAsync(userId, characterQuest, cancellationToken);
        }

        return characterQuest.Quest.Reward;
    }

    private async Task CollectItemsAsync(string userId, CharacterQuest characterQuest, CancellationToken cancellationToken)
    {
        var itemType = Enum.Parse<CharacterInventoryTypeEnum>(characterQuest.Quest.GameObjectName);

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
            inventory.Items.Remove(item);
        }
        else
        {
            item.Count -= characterQuest.Quest.Requirement;
        }

        characterInventory.Inventory = JsonSerializer.Serialize(inventory);

        Log.Debug("Collected items. DharacterInventoryId: {0}, UserId: {1}", characterInventory.Id, userId);
    }
}
