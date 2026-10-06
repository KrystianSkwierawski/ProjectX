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

        var result = await handler.Handle(new(HideoutBuildingEnum.Farm), default);

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

        var repeated = await Handler(context, clock).Handle(new(HideoutBuildingEnum.Farm), default);

        Assert.Equal("AlreadyBuilt", repeated.Outcome);
        Assert.Equal(2, repeated.CharacterInventory!.Inventory.Items[0].Count);
        Assert.Single(await context.CharacterHideouts.ToArrayAsync());
        Assert.False(context.ChangeTracker.HasChanges());
    }

    [Fact]
    public async Task InsufficientMaterials_PersistsNothing()
    {
        await using var context = await CreateContext(4);

        var result = await Handler(context, new Clock()).Handle(new(HideoutBuildingEnum.Farm), default);

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
        var result = await Handler(context, new Clock()).Handle(new(HideoutBuildingEnum.Farm), default);

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
            .Handle(new(HideoutBuildingEnum.Farm), default));

        Assert.Empty(await context.CharacterHideouts.ToArrayAsync());
    }

    [Fact]
    public async Task Farm_ConsumesOneSeedOffline_AndHarvestsAtomicallyWithoutReplay()
    {
        await using var context = await CreateContext(5);
        var clock = new Clock();
        var built = await Handler(context, clock).Handle(new(HideoutBuildingEnum.Farm), default);
        var inventory = await context.CharacterInventories.SingleAsync();
        inventory.Inventory.Add(InventoryItemEnum.StrawberrySeed, 1024, inventory.Count);

        await context.SaveChangesAsync();
        clock.Now = clock.Now.AddSeconds(5);
        var planted = await Handler(context, clock).Handle(new(HideoutBuildingEnum.Farm,
            HideoutOperationEnum.Deposit, 0, 0, InventoryItemEnum.StrawberrySeed, 1024,
            built.Buildings.Single().Farm!.Revision, 60), default);

        Assert.Equal("Applied", planted.Outcome);
        Assert.Equal(1024, planted.Buildings.Single().Farm!.Slots[0].Seeds.Count);
        Assert.Equal(0, planted.CharacterInventory!.Inventory.Items[0].Count);

        context.ChangeTracker.Clear();
        clock.Now = clock.Now.AddDays(7);
        var restored = await Handler(context, clock).Handle(new(), default);
        var farm = restored.Buildings.Single().Farm!;

        Assert.Equal(1023, farm.Slots[0].Seeds.Count);
        Assert.Equal(InventoryItemEnum.Strawberry, farm.Slots[0].Ready);
        Assert.Null(farm.Slots[0].ReadyAt);

        var request = new AccessHideoutCommand(HideoutBuildingEnum.Farm, HideoutOperationEnum.Harvest,
            Slot: 0, Revision: farm.Revision, RespawnInterval: 60, HarvestExperience: 50);
        var harvested = await Handler(context, clock).Handle(request, default);
        var repeated = await Handler(context, clock).Handle(request, default);

        context.ChangeTracker.Clear();
        var experience = await context.CharacterExperiences
            .Where(x => x.CharacterId == 41)
            .Where(x => x.Type == ExperienceTypeEnum.Herbalism)
            .ToArrayAsync();

        Assert.Single(experience);
        Assert.Equal(50, experience[0].Amount);
        Assert.Equal(harvested.HerbalismLevel, repeated.HerbalismLevel);
        Assert.Equal("Applied", harvested.Outcome);
        Assert.Equal("StaleState", repeated.Outcome);
        Assert.Equal(1, repeated.CharacterInventory!.Inventory.Items.Sum(x => x.Count));
        Assert.Equal(InventoryItemEnum.None, harvested.Buildings.Single().Farm!.Slots[0].Ready);
        Assert.Equal(clock.Now.AddSeconds(60), harvested.Buildings.Single().Farm!.Slots[0].ReadyAt);
    }

    [Fact]
    public async Task Farm_UpgradeCostsOnce_UnlocksOnlyAfterDeadline_AndStopsAtSixSlots()
    {
        await using var context = await CreateContext(5);
        var clock = new Clock();
        var built = await Handler(context, clock).Handle(new(HideoutBuildingEnum.Farm), default);
        var inventory = await context.CharacterInventories.SingleAsync();
        inventory.Inventory.Add(InventoryItemEnum.Wood, 30, inventory.Count);
        inventory.Inventory.Add(InventoryItemEnum.CopperBar, 7, inventory.Count);

        await context.SaveChangesAsync();
        var duringConstruction = await Handler(context, clock).Handle(new(HideoutBuildingEnum.Farm,
            HideoutOperationEnum.Upgrade, Revision: built.Buildings.Single().Farm!.Revision), default);

        Assert.Equal("Unavailable", duringConstruction.Outcome);

        clock.Now = clock.Now.AddSeconds(5);

        for (var level = 1; level < 3; level++)
        {
            var before = await Handler(context, clock).Handle(new(), default);
            var request = new AccessHideoutCommand(HideoutBuildingEnum.Farm, HideoutOperationEnum.Upgrade,
                Revision: before.Buildings.Single().Farm!.Revision);
            var upgrading = await Handler(context, clock).Handle(request, default);
            var duplicate = await Handler(context, clock).Handle(request, default);

            Assert.Equal("Applied", upgrading.Outcome);
            Assert.Equal("StaleState", duplicate.Outcome);
            Assert.Equal(level * 2, upgrading.Buildings.Single().Farm!.Capacity);

            var duringUpgrade = await Handler(context, clock).Handle(request with
            {
                Revision = upgrading.Buildings.Single().Farm!.Revision
            }, default);

            Assert.Equal("Unavailable", duringUpgrade.Outcome);

            clock.Now = upgrading.Buildings.Single().Farm!.UpgradeEndsAt!.Value;
            context.ChangeTracker.Clear();
            var finished = await Handler(context, clock).Handle(new(), default);

            Assert.Equal((level + 1) * 2, finished.Buildings.Single().Farm!.Capacity);
        }

        var maximum = await Handler(context, clock).Handle(new(), default);
        var rejected = await Handler(context, clock).Handle(new(HideoutBuildingEnum.Farm,
            HideoutOperationEnum.Upgrade, Revision: maximum.Buildings.Single().Farm!.Revision), default);

        Assert.Equal("Unavailable", rejected.Outcome);
        Assert.All(rejected.CharacterInventory!.Inventory.Items, x => Assert.Equal(0, x.Count));
    }

    [Fact]
    public async Task Farm_FullInventoryPreservesCrop_AndLockedSlotsRejectDeposits()
    {
        await using var context = await CreateContext(5);
        var clock = new Clock();
        var built = await Handler(context, clock).Handle(new(HideoutBuildingEnum.Farm), default);
        var inventory = await context.CharacterInventories.SingleAsync();
        inventory.Inventory.Add(InventoryItemEnum.MintSeed, 1, inventory.Count);

        await context.SaveChangesAsync();
        clock.Now = clock.Now.AddSeconds(5);
        var rejected = await Handler(context, clock).Handle(new(HideoutBuildingEnum.Farm,
            HideoutOperationEnum.Deposit, 2, 0, InventoryItemEnum.MintSeed, 1,
            built.Buildings.Single().Farm!.Revision), default);
        var planted = await Handler(context, clock).Handle(new(HideoutBuildingEnum.Farm,
            HideoutOperationEnum.Deposit, 0, 0, InventoryItemEnum.MintSeed, 1,
            built.Buildings.Single().Farm!.Revision), default);

        Assert.Equal("Unavailable", rejected.Outcome);
        Assert.Equal("Applied", planted.Outcome);
        inventory.Inventory.Add(InventoryItemEnum.Wood, 4096, inventory.Count);

        await context.SaveChangesAsync();
        clock.Now = clock.Now.AddSeconds(5);
        var ready = await Handler(context, clock).Handle(new(), default);
        var full = await Handler(context, clock).Handle(new(HideoutBuildingEnum.Farm,
            HideoutOperationEnum.Harvest, Slot: 0, Revision: ready.Buildings.Single().Farm!.Revision), default);

        Assert.Equal("InventoryFull", full.Outcome);
        Assert.Equal(InventoryItemEnum.Mint, full.Buildings.Single().Farm!.Slots[0].Ready);
        Assert.Equal(InventoryItemEnum.None, full.Buildings.Single().Farm!.Slots[0].Seeds.Type);
    }

    [Fact]
    public async Task Farm_WithdrawCancelsGrowth_AndFullSeedSlotRejectsWholeStack()
    {
        await using var context = await CreateContext(5);
        var clock = new Clock();
        var built = await Handler(context, clock).Handle(new(HideoutBuildingEnum.Farm), default);
        var inventory = await context.CharacterInventories.SingleAsync();
        inventory.Inventory.Add(InventoryItemEnum.ChamomileSeed, 1025, inventory.Count);

        await context.SaveChangesAsync();
        clock.Now = clock.Now.AddSeconds(5);
        var planted = await Handler(context, clock).Handle(new(HideoutBuildingEnum.Farm,
            HideoutOperationEnum.Deposit, 0, 0, InventoryItemEnum.ChamomileSeed, 1024,
            built.Buildings.Single().Farm!.Revision), default);
        var full = await Handler(context, clock).Handle(new(HideoutBuildingEnum.Farm,
            HideoutOperationEnum.Deposit, 0, 1, InventoryItemEnum.ChamomileSeed, 1,
            planted.Buildings.Single().Farm!.Revision), default);

        Assert.Equal("InventoryFull", full.Outcome);
        Assert.Equal(1, full.CharacterInventory!.Inventory.Items[1].Count);
        Assert.Equal(1024, full.Buildings.Single().Farm!.Slots[0].Seeds.Count);

        var withdrawn = await Handler(context, clock).Handle(new(HideoutBuildingEnum.Farm,
            HideoutOperationEnum.Withdraw, Slot: 0, Revision: full.Buildings.Single().Farm!.Revision), default);
        clock.Now = clock.Now.AddHours(1);
        context.ChangeTracker.Clear();
        var restored = await Handler(context, clock).Handle(new(), default);

        Assert.Equal("Applied", withdrawn.Outcome);
        Assert.Equal(1025, restored.CharacterInventory!.Inventory.Items.Sum(x => x.Count));
        Assert.Equal(InventoryItemEnum.None, restored.Buildings.Single().Farm!.Slots[0].Ready);
        Assert.Null(restored.Buildings.Single().Farm!.Slots[0].ReadyAt);
    }

    private static async Task<ApplicationDbContext> CreateContext(int count)
    {
        var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        context.HideoutBuildingTypes.Add(new HideoutBuildingType
        {
            Id = HideoutBuildingEnum.Farm,
            Name = "Farm",
            BuildTime = HideoutBuildingEnum.Farm.GetParameters().BuildTime,
            Requirement = HideoutBuildingEnum.Farm.GetParameters().Requirement,
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

        return new(context, user.Object, clock, NullLogger<AccessHideoutCommandHandler>.Instance, new HideoutStateSerializer());
    }

    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now = new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => Now;
    }
}
