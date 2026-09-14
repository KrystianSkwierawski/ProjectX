using Microsoft.EntityFrameworkCore;
using Moq;
using ProjectX.Application.CharacterInventories.Commands.ResolveCharacterInventoryTrade;
using ProjectX.Application.CharacterInventories.Commands.TradeCharacterInventories;
using ProjectX.Application.CharacterInventories.Queries.GetCharacterInventory;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Application.GameSessions.Models;
using ProjectX.Domain.Entities;
using ProjectX.Domain.Enums;
using ProjectX.Domain.Inventory;
using ProjectX.Infrastructure.Persistance;

namespace ProjectX.Infrastructure.IntegrationTests.Application;

public class TradeCharacterInventoriesTests
{
    private const string ServerUserId = "server-user";
    private const string SourceUserId = "source-user";
    private const string TargetUserId = "target-user";
    private const string TargetPlayerSessionId = "target-session";
    private const int SourceCharacterId = 41;
    private const int TargetCharacterId = 42;

    [Fact]
    public async Task Trade_AtomicallyExchangesBothOffersAndUsesStackRules()
    {
        await using var context = CreateContext();
        var source = CreateCharacter(
            SourceCharacterId,
            SourceUserId,
            capacity: 3,
            new InventorySlot(InventoryItemEnum.HealthPotion, 1000),
            new InventorySlot(InventoryItemEnum.Currency, 30));
        var target = CreateCharacter(
            TargetCharacterId,
            TargetUserId,
            capacity: 3,
            new InventorySlot(InventoryItemEnum.HealthPotion, 100),
            new InventorySlot(InventoryItemEnum.Fish, 5));

        context.Characters.AddRange(source, target);
        await context.SaveChangesAsync();

        var result = await CreateHandler(context).Handle(
            new TradeCharacterInventoriesCommand(
                Guid.NewGuid(),
                TargetPlayerSessionId,
                [new InventoryItemDto { Type = InventoryItemEnum.Currency, Count = 20 }],
                [new InventoryItemDto { Type = InventoryItemEnum.HealthPotion, Count = 100 }]),
            CancellationToken.None);

        Assert.Equal(TradeCharacterInventoriesStatusEnum.Applied, result.Status);
        Assert.Equal(1100, GetCount(source, InventoryItemEnum.HealthPotion));
        Assert.Equal(10, GetCount(source, InventoryItemEnum.Currency));
        Assert.Equal(20, GetCount(target, InventoryItemEnum.Currency));
        Assert.Equal(5, GetCount(target, InventoryItemEnum.Fish));
        Assert.Equal(1024, source.CharacterInventory.Inventory.Items[0].Count);
        Assert.Equal(76, source.CharacterInventory.Inventory.Items[2].Count);
        Assert.NotNull(result.SourceInventory);
        Assert.NotNull(result.TargetInventory);
        Assert.Equal(1100, GetCount(result.SourceInventory, InventoryItemEnum.HealthPotion));
        Assert.Equal(20, GetCount(result.TargetInventory, InventoryItemEnum.Currency));
    }

    [Fact]
    public async Task Trade_AtomicallyResynchronizesCollectQuestProgressForBothCharacters()
    {
        await using var context = CreateContext();
        var source = CreateCharacter(
            SourceCharacterId,
            SourceUserId,
            capacity: 2,
            new InventorySlot(InventoryItemEnum.HealthPotion, 10));
        var target = CreateCharacter(
            TargetCharacterId,
            TargetUserId,
            capacity: 2,
            InventorySlot.Empty());
        var quest = new Quest
        {
            Id = QuestEnum.Collect2Cans,
            Name = nameof(QuestEnum.Collect2Cans),
            Type = QuestTypeEnum.Collect,
            GameObjectName = InventoryItemEnum.HealthPotion.ToString(),
            Requirement = 9,
            Status = StatusEnum.Active
        };
        var sourceQuest = CreateCharacterQuest(51, source, quest, CharacterQuestStatusEnum.Finished, progress: 10);
        var targetQuest = CreateCharacterQuest(52, target, quest, CharacterQuestStatusEnum.Accepted, progress: 0);

        context.AddRange(source, target, quest, sourceQuest, targetQuest);
        await context.SaveChangesAsync();

        var result = await CreateHandler(context).Handle(
            new TradeCharacterInventoriesCommand(
                Guid.NewGuid(),
                TargetPlayerSessionId,
                [new InventoryItemDto { Type = InventoryItemEnum.HealthPotion, Count = 2 }],
                []),
            CancellationToken.None);

        context.ChangeTracker.Clear();
        var persistedSourceQuest = await context.CharacterQuests.SingleAsync(x => x.Id == sourceQuest.Id);
        var persistedTargetQuest = await context.CharacterQuests.SingleAsync(x => x.Id == targetQuest.Id);

        Assert.Equal(TradeCharacterInventoriesStatusEnum.Applied, result.Status);
        Assert.Equal(8, persistedSourceQuest.Progress);
        Assert.Equal(CharacterQuestStatusEnum.Accepted, persistedSourceQuest.Status);
        Assert.Equal(2, persistedTargetQuest.Progress);
        Assert.Equal(CharacterQuestStatusEnum.Accepted, persistedTargetQuest.Status);
    }

    [Fact]
    public async Task Trade_LeavesBothInventoriesUnchangedWhenTargetCannotFitTheOffer()
    {
        await using var context = CreateContext();
        var source = CreateCharacter(
            SourceCharacterId,
            SourceUserId,
            capacity: 1,
            new InventorySlot(InventoryItemEnum.HealthPotion, 2));
        var target = CreateCharacter(
            TargetCharacterId,
            TargetUserId,
            capacity: 1,
            new InventorySlot(InventoryItemEnum.Currency, InventorySlot.MaxStackSize));

        context.Characters.AddRange(source, target);
        await context.SaveChangesAsync();

        var result = await CreateHandler(context).Handle(
            new TradeCharacterInventoriesCommand(
                Guid.NewGuid(),
                TargetPlayerSessionId,
                [new InventoryItemDto { Type = InventoryItemEnum.HealthPotion, Count = 1 }],
                []),
            CancellationToken.None);

        Assert.Equal(TradeCharacterInventoriesStatusEnum.TargetInventoryFull, result.Status);
        Assert.Equal(2, GetCount(source, InventoryItemEnum.HealthPotion));
        Assert.Equal(InventorySlot.MaxStackSize, GetCount(target, InventoryItemEnum.Currency));
        Assert.Equal(0, GetCount(target, InventoryItemEnum.HealthPotion));
        Assert.Null(result.SourceInventory);
        Assert.Null(result.TargetInventory);
    }

    [Fact]
    public async Task Trade_LeavesBothInventoriesUnchangedWhenAnOfferedItemIsUnavailable()
    {
        await using var context = CreateContext();
        var source = CreateCharacter(
            SourceCharacterId,
            SourceUserId,
            capacity: 2,
            new InventorySlot(InventoryItemEnum.HealthPotion, 1));
        var target = CreateCharacter(
            TargetCharacterId,
            TargetUserId,
            capacity: 2,
            new InventorySlot(InventoryItemEnum.Currency, 10));

        context.Characters.AddRange(source, target);
        await context.SaveChangesAsync();

        var result = await CreateHandler(context).Handle(
            new TradeCharacterInventoriesCommand(
                Guid.NewGuid(),
                TargetPlayerSessionId,
                [new InventoryItemDto { Type = InventoryItemEnum.HealthPotion, Count = 2 }],
                [new InventoryItemDto { Type = InventoryItemEnum.Currency, Count = 5 }]),
            CancellationToken.None);

        Assert.Equal(TradeCharacterInventoriesStatusEnum.SourceItemsUnavailable, result.Status);
        Assert.Equal(1, GetCount(source, InventoryItemEnum.HealthPotion));
        Assert.Equal(10, GetCount(target, InventoryItemEnum.Currency));
    }

    [Fact]
    public async Task Trade_ReturnsPreviousSuccessWithoutApplyingTheSameCommitTwice()
    {
        await using var context = CreateContext();
        var source = CreateCharacter(
            SourceCharacterId,
            SourceUserId,
            capacity: 2,
            new InventorySlot(InventoryItemEnum.HealthPotion, 10));
        var target = CreateCharacter(
            TargetCharacterId,
            TargetUserId,
            capacity: 2,
            new InventorySlot(InventoryItemEnum.Currency, 20));
        var command = new TradeCharacterInventoriesCommand(
            Guid.NewGuid(),
            TargetPlayerSessionId,
            [new InventoryItemDto { Type = InventoryItemEnum.HealthPotion, Count = 2 }],
            [new InventoryItemDto { Type = InventoryItemEnum.Currency, Count = 3 }]);

        context.Characters.AddRange(source, target);
        await context.SaveChangesAsync();

        var firstResult = await CreateHandler(context).Handle(command, CancellationToken.None);
        var secondResult = await CreateHandler(context).Handle(command, CancellationToken.None);

        Assert.Equal(TradeCharacterInventoriesStatusEnum.Applied, firstResult.Status);
        Assert.Equal(TradeCharacterInventoriesStatusEnum.Applied, secondResult.Status);
        Assert.Equal(8, GetCount(source, InventoryItemEnum.HealthPotion));
        Assert.Equal(3, GetCount(source, InventoryItemEnum.Currency));
        Assert.Equal(2, GetCount(target, InventoryItemEnum.HealthPotion));
        Assert.Equal(17, GetCount(target, InventoryItemEnum.Currency));
        Assert.Single(context.CharacterInventoryTradeReceipts);
        Assert.NotNull(secondResult.SourceInventory);
        Assert.NotNull(secondResult.TargetInventory);
        Assert.Equal(8, GetCount(secondResult.SourceInventory, InventoryItemEnum.HealthPotion));
        Assert.Equal(17, GetCount(secondResult.TargetInventory, InventoryItemEnum.Currency));
    }

    [Fact]
    public async Task ResolveTrade_ReturnsAuthoritativeInventoriesForAnAppliedReceipt()
    {
        await using var context = CreateContext();
        var source = CreateCharacter(
            SourceCharacterId,
            SourceUserId,
            capacity: 2,
            new InventorySlot(InventoryItemEnum.HealthPotion, 10));
        var target = CreateCharacter(
            TargetCharacterId,
            TargetUserId,
            capacity: 2,
            new InventorySlot(InventoryItemEnum.Currency, 20));
        var tradeId = Guid.NewGuid();

        context.Characters.AddRange(source, target);
        await context.SaveChangesAsync();

        await CreateHandler(context).Handle(
            new TradeCharacterInventoriesCommand(
                tradeId,
                TargetPlayerSessionId,
                [new InventoryItemDto { Type = InventoryItemEnum.HealthPotion, Count = 2 }],
                [new InventoryItemDto { Type = InventoryItemEnum.Currency, Count = 3 }]),
            CancellationToken.None);

        var result = await new ResolveCharacterInventoryTradeCommandHandler(context).Handle(
            new ResolveCharacterInventoryTradeCommand(tradeId),
            CancellationToken.None);

        Assert.Equal(TradeCharacterInventoriesStatusEnum.Applied, result.Status);
        Assert.NotNull(result.SourceInventory);
        Assert.NotNull(result.TargetInventory);
        Assert.Equal(8, GetCount(result.SourceInventory, InventoryItemEnum.HealthPotion));
        Assert.Equal(3, GetCount(result.SourceInventory, InventoryItemEnum.Currency));
        Assert.Equal(2, GetCount(result.TargetInventory, InventoryItemEnum.HealthPotion));
        Assert.Equal(17, GetCount(result.TargetInventory, InventoryItemEnum.Currency));
    }

    [Fact]
    public async Task ResolveTrade_ReturnsReceiptNotFoundForAnUnknownCommit()
    {
        await using var context = CreateContext();

        var result = await new ResolveCharacterInventoryTradeCommandHandler(context).Handle(
            new ResolveCharacterInventoryTradeCommand(Guid.NewGuid()),
            CancellationToken.None);

        Assert.Equal(TradeCharacterInventoriesStatusEnum.ReceiptNotFound, result.Status);
        Assert.Null(result.SourceInventory);
        Assert.Null(result.TargetInventory);
    }

    [Fact]
    public async Task Trade_RejectsReusingACommitIdentifierForDifferentOffers()
    {
        await using var context = CreateContext();
        var source = CreateCharacter(
            SourceCharacterId,
            SourceUserId,
            capacity: 2,
            new InventorySlot(InventoryItemEnum.HealthPotion, 10));
        var target = CreateCharacter(
            TargetCharacterId,
            TargetUserId,
            capacity: 2,
            new InventorySlot(InventoryItemEnum.Currency, 20));
        var tradeId = Guid.NewGuid();

        context.Characters.AddRange(source, target);
        await context.SaveChangesAsync();

        await CreateHandler(context).Handle(
            new TradeCharacterInventoriesCommand(
                tradeId,
                TargetPlayerSessionId,
                [new InventoryItemDto { Type = InventoryItemEnum.HealthPotion, Count = 2 }],
                []),
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => CreateHandler(context).Handle(
            new TradeCharacterInventoriesCommand(
                tradeId,
                TargetPlayerSessionId,
                [new InventoryItemDto { Type = InventoryItemEnum.HealthPotion, Count = 3 }],
                []),
            CancellationToken.None));

        Assert.Contains("reused", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(8, GetCount(source, InventoryItemEnum.HealthPotion));
        Assert.Equal(2, GetCount(target, InventoryItemEnum.HealthPotion));
    }

    [Fact]
    public void TradeValidator_RejectsUnknownInventoryItemTypes()
    {
        var validator = new TradeCharacterInventoriesCommandValidator();
        var command = new TradeCharacterInventoriesCommand(
            Guid.NewGuid(),
            TargetPlayerSessionId,
            [new InventoryItemDto { Type = (InventoryItemEnum)int.MaxValue, Count = 1 }],
            []);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }

    private static TradeCharacterInventoriesCommandHandler CreateHandler(ApplicationDbContext context)
    {
        var sessions = new Mock<IGameSessionService>();
        sessions
            .Setup(x => x.TryResolvePlayer(ServerUserId, TargetPlayerSessionId, out It.Ref<ResolvedPlayerSession>.IsAny))
            .Returns((string _, string _, out ResolvedPlayerSession resolved) =>
            {
                resolved = new ResolvedPlayerSession(TargetUserId, TargetCharacterId);
                return true;
            });

        return new TradeCharacterInventoriesCommandHandler(context, new TestCurrentUserService(), sessions.Object);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static Character CreateCharacter(int id, string userId, short capacity, params InventorySlot[] inventory)
    {
        return new Character
        {
            Id = id,
            ApplicationUserId = userId,
            Name = $"{userId}-character",
            Health = 100,
            MaxHealth = 100,
            Status = StatusEnum.Active,
            CharacterInventory = new CharacterInventory
            {
                Id = id,
                Inventory = new InventoryState(inventory),
                Count = capacity
            }
        };
    }

    private static CharacterQuest CreateCharacterQuest(
        int id,
        Character character,
        Quest quest,
        CharacterQuestStatusEnum status,
        int progress)
    {
        return new CharacterQuest
        {
            Id = id,
            CharacterId = character.Id,
            Character = character,
            QuestId = quest.Id,
            Quest = quest,
            Status = status,
            Progress = progress,
            StartDate = DateTimeOffset.UtcNow
        };
    }

    private static int GetCount(Character character, InventoryItemEnum type)
    {
        return character.CharacterInventory.Inventory.GetCount(type);
    }

    private static int GetCount(CharacterInventoryDto inventory, InventoryItemEnum type)
    {
        return inventory.Inventory.Items
            .Where(x => x.Type == type)
            .Sum(x => x.Count);
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public LanguageEnum Language => LanguageEnum.en;

        public List<string>? Roles => [];

        public string GetId() => SourceUserId;

        public string GetAuthenticatedUserId() => ServerUserId;

        public int? GetCharacterId() => SourceCharacterId;

        public DateTimeOffset? GetAuthenticatedSessionStartedAtUtc() => null;

        public DateTimeOffset? GetAuthenticatedTokenExpirationUtc() => null;
    }
}
