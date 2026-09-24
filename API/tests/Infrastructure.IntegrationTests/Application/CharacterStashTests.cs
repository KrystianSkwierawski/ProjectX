using Microsoft.EntityFrameworkCore;
using Moq;
using ProjectX.Application.CharacterStashes;
using ProjectX.Application.Common.Exceptions;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Entities;
using ProjectX.Domain.Enums;
using ProjectX.Domain.Inventory;
using ProjectX.Infrastructure.Persistance;

namespace ProjectX.Infrastructure.IntegrationTests.Application;

public class CharacterStashTests
{
    [Fact]
    public async Task Stash_IsSharedByAccountAndRejectsForeignCharacter()
    {
        await using var context = CreateContext();
        context.Characters.AddRange(Character(41, "owner", 10), Character(42, "owner", 0), Character(99, "foreign", 0));

        await context.SaveChangesAsync();

        var first = Handler(context, 41);
        var empty = await first.Handle(new(StashOperationEnum.Read), default);

        Assert.Equal(64, empty.Count);
        Assert.Empty(empty.Inventory!.Items);

        var stored = await first.Handle(new(StashOperationEnum.Deposit, 0, 0, 63, InventoryItemEnum.HealthPotion, 10), default);
        var shared = await Handler(context, 42).Handle(new(StashOperationEnum.Read), default);

        Assert.Equal(StashStatusEnum.Applied, stored.Status);
        Assert.Equal(10, shared.Inventory!.Items[63].Count);
        Assert.Equal(64, shared.Inventory.Items.Count);
        Assert.Equal(1, shared.Revision);

        await Assert.ThrowsAsync<NotFoundException>(() => Handler(context, 99).Handle(new(StashOperationEnum.Read), default));
    }

    [Fact]
    public async Task Transfer_UpdatesCollectInBothDirectionsAndRejectsReplay()
    {
        await using var context = CreateContext();
        var character = Character(41, "owner", 10);
        var quest = new Quest
        {
            Id = QuestEnum.Collect2Cans,
            Name = "Collect",
            Type = QuestTypeEnum.Collect,
            GameObjectName = nameof(InventoryItemEnum.HealthPotion),
            Requirement = 9
        };
        var progress = new CharacterQuest
        {
            Character = character,
            Quest = quest,
            Status = CharacterQuestStatusEnum.Finished,
            Progress = 10
        };
        context.AddRange(character, quest, progress);

        await context.SaveChangesAsync();

        var handler = Handler(context, 41);
        var deposit = new AccessCharacterStashCommand(StashOperationEnum.Deposit, 0, 0, null, InventoryItemEnum.HealthPotion, 10);
        var result = await handler.Handle(deposit, default);

        Assert.Equal(StashStatusEnum.Applied, result.Status);
        Assert.Equal(0, progress.Progress);
        Assert.Equal(CharacterQuestStatusEnum.Accepted, progress.Status);
        Assert.Equal(StashStatusEnum.Changed, (await handler.Handle(deposit, default)).Status);

        result = await handler.Handle(new(StashOperationEnum.Withdraw, 1, 0, null, InventoryItemEnum.HealthPotion, 10), default);

        Assert.Equal(StashStatusEnum.Applied, result.Status);
        Assert.Equal(10, progress.Progress);
        Assert.Equal(CharacterQuestStatusEnum.Finished, progress.Status);
        Assert.True(result.Inventory!.Items[0].Type == InventoryItemEnum.None);
    }

    [Theory]
    [InlineData(StashOperationEnum.Deposit)]
    [InlineData(StashOperationEnum.Withdraw)]
    public async Task FullDestination_PreservesBothInventories(StashOperationEnum operation)
    {
        await using var context = CreateContext();
        var character = Character(41, "owner", operation == StashOperationEnum.Deposit ? 30 : 1024);
        character.CharacterInventory.Count = 1;
        var stash = new CharacterStash
        {
            ApplicationUserId = "owner",
            Count = 1,
            Inventory = new([new InventorySlot(InventoryItemEnum.HealthPotion, operation == StashOperationEnum.Deposit ? 1024 : 30)])
        };
        context.AddRange(character, stash);

        await context.SaveChangesAsync();

        var result = await Handler(context, 41).Handle(new(operation, 0, 0, null, InventoryItemEnum.HealthPotion, 30), default);

        Assert.Equal(StashStatusEnum.Full, result.Status);
        Assert.Equal(1054, character.CharacterInventory.Inventory.GetCount(InventoryItemEnum.HealthPotion) + stash.Inventory.GetCount(InventoryItemEnum.HealthPotion));
        Assert.Equal(0, stash.Revision);
        Assert.False(context.ChangeTracker.HasChanges());
    }

    [Fact]
    public async Task SplitMoveAndStaleSource_PreserveItemsAndPositions()
    {
        await using var context = CreateContext();
        context.AddRange(Character(41, "owner", 0), new CharacterStash
        {
            ApplicationUserId = "owner",
            Inventory = new([new InventorySlot(InventoryItemEnum.HealthPotion, 11)])
        });

        await context.SaveChangesAsync();

        var handler = Handler(context, 41);
        var result = await handler.Handle(new(StashOperationEnum.Split, 0, 0, null, InventoryItemEnum.HealthPotion, 11), default);

        Assert.Equal(new[] { 6, 5 }, result.Inventory!.Items.Select(x => x.Count));

        result = await handler.Handle(new(StashOperationEnum.Move, 1, 1, 63, InventoryItemEnum.HealthPotion, 5), default);

        Assert.Equal(0, result.Inventory!.Items[1].Count);
        Assert.Equal(5, result.Inventory.Items[63].Count);

        result = await handler.Handle(new(StashOperationEnum.Withdraw, 2, 63, null, InventoryItemEnum.HealthPotion, 6), default);

        Assert.Equal(StashStatusEnum.Changed, result.Status);
        Assert.Equal(11, (await context.CharacterStashes.SingleAsync()).Inventory.GetCount(InventoryItemEnum.HealthPotion));
    }

    [Fact]
    public async Task ConcurrentAccountRevision_RejectsLostUpdate()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var first = new ApplicationDbContext(options);
        first.AddRange(Character(41, "owner", 10), Character(42, "owner", 20), new CharacterStash { ApplicationUserId = "owner" });

        await first.SaveChangesAsync();

        await using var second = new ApplicationDbContext(options);
        await Handler(second, 42).Handle(new(StashOperationEnum.Read), default);
        await Handler(first, 41).Handle(new(StashOperationEnum.Deposit, 0, 0, null, InventoryItemEnum.HealthPotion, 10), default);

        var stale = await Handler(second, 42).Handle(new(StashOperationEnum.Deposit, 0, 0, null, InventoryItemEnum.HealthPotion, 20), default);

        Assert.Equal(StashStatusEnum.Changed, stale.Status);
    }

    private static ApplicationDbContext CreateContext() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static AccessCharacterStashCommandHandler Handler(ApplicationDbContext context, int characterId)
    {
        var user = new Mock<ICurrentUserService>();
        user.Setup(x => x.GetId()).Returns("owner");
        user.Setup(x => x.GetCharacterId()).Returns(characterId);

        return new(context, user.Object);
    }

    private static Character Character(int id, string userId, int count) => new()
    {
        Id = id,
        Name = $"Character{id}",
        ApplicationUserId = userId,
        CharacterInventory = new CharacterInventory
        {
            Id = id,
            Count = 4,
            Inventory = new(count == 0 ? [] : [new InventorySlot(InventoryItemEnum.HealthPotion, count)])
        }
    };
}
