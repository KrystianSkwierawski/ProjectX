using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ProjectX.Application.Common.Exceptions;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Application.Hideouts;
using ProjectX.Domain.Entities;
using ProjectX.Domain.Enums;
using ProjectX.Domain.Inventory;
using ProjectX.Infrastructure.Persistance;

namespace ProjectX.Infrastructure.IntegrationTests.Application;

public class HideoutTests
{
    [Fact]
    public async Task Construction_ConsumesCatalogCostOnce_AndSurvivesReloadAndElapsedTime()
    {
        await using var context = await CreateContext(7);
        var clock = new Clock();
        var handler = Handler(context, clock);

        var result = await handler.Handle(new(HideoutBuildingEnum.ChamomileFarm), default);

        Assert.Equal("Applied", result.Outcome);
        Assert.Equal(2, result.CharacterInventory!.Inventory.Items[0].Count);
        Assert.Equal(clock.Now.AddSeconds(5), result.Buildings.Single().BuildEndsAt);

        context.ChangeTracker.Clear();
        clock.Now = clock.Now.AddSeconds(4);
        var restored = await Handler(context, clock).Handle(new(), default);

        Assert.Equal(1, (restored.Buildings.Single().BuildEndsAt!.Value - restored.CurrentTime).TotalSeconds);
        Assert.True((await context.CharacterHideouts.SingleAsync()).IsUnderConstruction(clock.Now));

        clock.Now = clock.Now.AddSeconds(1);
        Assert.False((await context.CharacterHideouts.SingleAsync()).IsUnderConstruction(clock.Now));

        var repeated = await Handler(context, clock).Handle(new(HideoutBuildingEnum.ChamomileFarm), default);

        Assert.Equal("AlreadyBuilt", repeated.Outcome);
        Assert.Equal(2, repeated.CharacterInventory!.Inventory.Items[0].Count);
        Assert.Single(await context.CharacterHideouts.ToArrayAsync());
        Assert.False(context.ChangeTracker.HasChanges());
    }

    [Fact]
    public async Task InsufficientMaterials_PersistsNothing()
    {
        await using var context = await CreateContext(4);

        var result = await Handler(context, new Clock()).Handle(new(HideoutBuildingEnum.ChamomileFarm), default);

        Assert.Equal("MissingMaterials", result.Outcome);
        Assert.Equal(4, result.CharacterInventory!.Inventory.Items[0].Count);
        Assert.Empty(await context.CharacterHideouts.ToArrayAsync());
        Assert.False(context.ChangeTracker.HasChanges());
    }

    [Fact]
    public async Task Construction_ResynchronizesFinishedCollectQuest_AndPreservesEmptySlot()
    {
        await using var context = await CreateContext(5);
        var quest = new CharacterQuest
        {
            CharacterId = 41,
            Quest = new Quest
            {
                Id = QuestEnum.Collect2Cans,
                Name = "Collect chamomile",
                Type = QuestTypeEnum.Collect,
                GameObjectName = nameof(InventoryItemEnum.Chamomile),
                Requirement = 5
            },
            Progress = 5,
            Status = CharacterQuestStatusEnum.Finished
        };
        context.CharacterQuests.Add(quest);

        await context.SaveChangesAsync();
        var result = await Handler(context, new Clock()).Handle(new(HideoutBuildingEnum.ChamomileFarm), default);

        Assert.Equal(CharacterQuestStatusEnum.Accepted, quest.Status);
        Assert.Equal(0, quest.Progress);
        Assert.Equal(InventoryItemEnum.None, result.CharacterInventory!.Inventory.Items[0].Type);
        Assert.Equal(0, result.CharacterInventory.Inventory.Items[0].Count);
    }

    [Fact]
    public async Task ForeignSessionCharacter_IsRejected()
    {
        await using var context = await CreateContext(5);

        await Assert.ThrowsAsync<NotFoundException>(() => Handler(context, new Clock(), "foreign")
            .Handle(new(HideoutBuildingEnum.ChamomileFarm), default));

        Assert.Empty(await context.CharacterHideouts.ToArrayAsync());
    }

    private static async Task<ApplicationDbContext> CreateContext(int count)
    {
        var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        context.HideoutBuildingTypes.Add(new HideoutBuildingType
        {
            Id = HideoutBuildingEnum.ChamomileFarm,
            Name = "ChamomileFarm",
            BuildTime = HideoutBuildingEnum.ChamomileFarm.GetParameters().BuildTime,
            Requirement = HideoutBuildingEnum.ChamomileFarm.GetParameters().Requirement,
            Status = StatusEnum.Active
        });
        context.Characters.Add(new Character
        {
            Id = 41,
            Name = "Builder",
            ApplicationUserId = "owner",
            CharacterInventory = new CharacterInventory
            {
                Id = 41,
                Count = 4,
                Inventory = new([new InventorySlot(InventoryItemEnum.Chamomile, count)])
            }
        });

        await context.SaveChangesAsync();

        return context;
    }

    private static AccessHideoutCommandHandler Handler(ApplicationDbContext context, Clock clock, string owner = "owner")
    {
        var user = new Mock<ICurrentUserService>();
        user.Setup(x => x.GetId()).Returns(owner);
        user.Setup(x => x.GetCharacterId()).Returns(41);

        return new(context, user.Object, clock, NullLogger<AccessHideoutCommandHandler>.Instance);
    }

    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now = new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => Now;
    }
}
