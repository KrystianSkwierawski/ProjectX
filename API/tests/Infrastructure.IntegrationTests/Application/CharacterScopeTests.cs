using Microsoft.EntityFrameworkCore;
using ProjectX.Application.CharacterExperiences.Commands.AddCharacterExperience;
using ProjectX.Application.CharacterInventories.Commands.UpdateCharacterInventory;
using ProjectX.Application.CharacterInventories.Queries.GetCharacterInventory;
using ProjectX.Application.CharacterQuests.Commands.AcceptCharacterQuest;
using ProjectX.Application.CharacterQuests.Commands.CheckCharacterQuestProgress;
using ProjectX.Application.CharacterQuests.Commands.CompleteCharacterQuest;
using ProjectX.Application.CharacterQuests.Queries.GetCharacterQuests;
using ProjectX.Application.Characters.Commands;
using ProjectX.Application.Characters.Queries.GetCharacter;
using ProjectX.Application.Characters.Queries.GetCharacters;
using ProjectX.Application.Common.Exceptions;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Entities;
using ProjectX.Domain.Enums;
using ProjectX.Domain.Inventory;
using ProjectX.Infrastructure.Persistance;

namespace ProjectX.Infrastructure.IntegrationTests.Application;

public class CharacterScopeTests
{
    private const string CurrentUserId = "current-user";
    private const int CurrentCharacterId = 42;
    private const int OtherOwnedCharacterId = 43;
    private const int ForeignCharacterId = 1;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LayoutReceipt_ReplayDoesNotApplyMoveOrSplitTwice(bool split)
    {
        await using var context = CreateContext();
        var character = CreateCharacter(CurrentCharacterId, CurrentUserId,
            new InventorySlot(InventoryItemEnum.Can, 8),
            new InventorySlot(InventoryItemEnum.Wood, 3));
        context.Add(character);

        await context.SaveChangesAsync();

        var request = CreateLayoutRequest(split);
        var handler = new UpdateCharacterInventoryCommandHandler(context, new TestCurrentUserService());

        var first = await handler.Handle(request, CancellationToken.None);
        var replay = await handler.Handle(request, CancellationToken.None);

        Assert.Equal(UpdateCharacterInventoryStatusEnum.Applied, first.Status);
        Assert.Equal(first.Status, replay.Status);
        Assert.Equal(split ? InventoryItemEnum.Can : InventoryItemEnum.Wood, character.CharacterInventory.Inventory.Items[0].Type);
        Assert.Equal(split ? 4 : 3, character.CharacterInventory.Inventory.Items[0].Count);
        Assert.Equal(split ? 3 : 2, character.CharacterInventory.Inventory.Items.Count);
        Assert.Equal(8, character.CharacterInventory.Inventory.GetCount(InventoryItemEnum.Can));
        Assert.Single(await context.CharacterInventoryHealthReceipts.ToArrayAsync());

        var changed = split
            ? request with { SplitSlotIndex = 1 }
            : request with { MoveTargetSlotIndex = 2 };

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(changed, CancellationToken.None));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LayoutReceipt_OverlappingRetryResolvesBeforeOriginalSave(bool split)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var seed = new ApplicationDbContext(options);
        seed.Add(CreateCharacter(CurrentCharacterId, CurrentUserId,
            new InventorySlot(InventoryItemEnum.Can, 8),
            new InventorySlot(InventoryItemEnum.Wood, 3)));

        await seed.SaveChangesAsync();

        await using var delayed = new DelayedPotionContext(options);
        await using var retry = new ApplicationDbContext(options);
        var request = CreateLayoutRequest(split);
        var originalTask = new UpdateCharacterInventoryCommandHandler(delayed, new TestCurrentUserService())
            .Handle(request, CancellationToken.None);

        await delayed.SaveStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));

        var earlier = await retry.CharacterInventories
            .Where(x => x.Id == CurrentCharacterId)
            .SingleAsync();

        Assert.False(originalTask.IsCompleted);
        Assert.Equal(8, earlier.Inventory.Items[0].Count);

        retry.ChangeTracker.Clear();

        var resolved = await new UpdateCharacterInventoryCommandHandler(retry, new TestCurrentUserService())
            .Handle(request, CancellationToken.None);

        delayed.ResumeSave.SetResult();

        var original = await originalTask;

        Assert.Equal(UpdateCharacterInventoryStatusEnum.Applied, resolved.Status);
        Assert.Equal(resolved.Status, original.Status);

        retry.ChangeTracker.Clear();

        var stored = await retry.CharacterInventories
            .Where(x => x.Id == CurrentCharacterId)
            .SingleAsync();

        Assert.Equal(split ? 4 : 3, stored.Inventory.Items[0].Count);
        Assert.Equal(split ? InventoryItemEnum.Can : InventoryItemEnum.Wood, stored.Inventory.Items[0].Type);
        Assert.Single(await retry.CharacterInventoryHealthReceipts.ToArrayAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LayoutReceipt_RejectionRemainsFinalAfterSlotBecomesValid(bool split)
    {
        await using var context = CreateContext();
        var character = CreateCharacter(CurrentCharacterId, CurrentUserId);
        context.Add(character);

        await context.SaveChangesAsync();

        var request = CreateLayoutRequest(split);
        var handler = new UpdateCharacterInventoryCommandHandler(context, new TestCurrentUserService());

        var rejected = await handler.Handle(request, CancellationToken.None);

        character.CharacterInventory.Inventory.Add(InventoryItemEnum.Can, 8, 15);

        await context.SaveChangesAsync();

        var replay = await handler.Handle(request, CancellationToken.None);

        Assert.Equal(UpdateCharacterInventoryStatusEnum.OperationRejected, rejected.Status);
        Assert.Equal(rejected.Status, replay.Status);
        Assert.Single(character.CharacterInventory.Inventory.Items);
        Assert.Equal(8, character.CharacterInventory.Inventory.Items[0].Count);
    }

    [Fact]
    public async Task LayoutReceipt_RejectsMixedLayoutAndCharacterPayload()
    {
        await using var context = CreateContext();
        var handler = new UpdateCharacterInventoryCommandHandler(context, new TestCurrentUserService());
        var request = CreateLayoutRequest(false) with { CharacterUpdate = new UpdateCharacterCommand { Health = 50 } };

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(request, CancellationToken.None));

        Assert.Empty(await context.CharacterInventoryHealthReceipts.ToArrayAsync());
    }

    private static UpdateCharacterInventoryCommand CreateLayoutRequest(bool split) => split
        ? new([], [], SplitSlotIndex: 0, HealthOperationId: Guid.NewGuid())
        : new([], [], MoveSourceSlotIndex: 0, MoveTargetSlotIndex: 1, HealthOperationId: Guid.NewGuid());

    [Fact]
    public async Task DamageReceipt_ReplayPreservesLaterHealingAndEquipment()
    {
        await using var context = CreateContext();
        var character = CreateCharacter(CurrentCharacterId, CurrentUserId);
        character.Health = 100;
        context.Add(character);

        await context.SaveChangesAsync();

        var request = new UpdateCharacterInventoryCommand([], [],
            CharacterUpdate: new UpdateCharacterCommand { Health = 40 },
            HealthOperationId: Guid.NewGuid());
        var handler = new UpdateCharacterInventoryCommandHandler(context, new TestCurrentUserService());

        var applied = await handler.Handle(request, CancellationToken.None);

        Assert.Equal(UpdateCharacterInventoryStatusEnum.Applied, applied.Status);
        Assert.Equal(40, character.Health);

        character.Health = 70;
        character.Strength = 35;

        await context.SaveChangesAsync();

        var replay = await handler.Handle(request, CancellationToken.None);

        Assert.Equal(UpdateCharacterInventoryStatusEnum.Applied, replay.Status);
        Assert.Equal(70, character.Health);
        Assert.Equal(35, character.Strength);
        Assert.Single(await context.CharacterInventoryHealthReceipts.ToArrayAsync());
    }

    [Fact]
    public async Task PotionReceipt_ReplayDoesNotConsumeOrRestoreHealthAgain()
    {
        await using var context = CreateContext();
        var character = CreateCharacter(CurrentCharacterId, CurrentUserId,
            new InventorySlot(InventoryItemEnum.HealthPotion, 2));
        character.Health = 50;
        context.Add(character);

        await context.SaveChangesAsync();

        var request = CreatePotionRequest();
        var handler = new UpdateCharacterInventoryCommandHandler(context, new TestCurrentUserService());
        var first = await handler.Handle(request, CancellationToken.None);

        character.Health = 60;

        await context.SaveChangesAsync();

        var replay = await handler.Handle(request, CancellationToken.None);

        Assert.Equal(UpdateCharacterInventoryStatusEnum.Applied, first.Status);
        Assert.Equal(UpdateCharacterInventoryStatusEnum.Applied, replay.Status);
        Assert.Equal(60, character.Health);
        Assert.Equal(1, character.CharacterInventory.Inventory.GetCount(InventoryItemEnum.HealthPotion));
        Assert.Single(await context.CharacterInventoryHealthReceipts.ToArrayAsync());

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
            request with { CharacterUpdate = new UpdateCharacterCommand { Health = 80 } }, CancellationToken.None));
    }

    [Fact]
    public async Task PotionReceipt_RejectedReplayRemainsRejectedAfterInventoryChanges()
    {
        await using var context = CreateContext();
        var character = CreateCharacter(CurrentCharacterId, CurrentUserId);
        character.Health = 50;
        context.Add(character);

        await context.SaveChangesAsync();

        var request = CreatePotionRequest();
        var handler = new UpdateCharacterInventoryCommandHandler(context, new TestCurrentUserService());
        var first = await handler.Handle(request, CancellationToken.None);

        character.CharacterInventory.Inventory.Add(InventoryItemEnum.HealthPotion, 1, 15);

        await context.SaveChangesAsync();

        var replay = await handler.Handle(request, CancellationToken.None);

        Assert.Equal(UpdateCharacterInventoryStatusEnum.OperationRejected, first.Status);
        Assert.Equal(first.Status, replay.Status);
        Assert.Equal(50, character.Health);
        Assert.Equal(1, character.CharacterInventory.Inventory.GetCount(InventoryItemEnum.HealthPotion));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CharacterStateReceipt_RetryFinishesWhileOriginalWriteIsStillPending(bool equipment)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var seed = new ApplicationDbContext(options);
        var itemType = equipment ? InventoryItemEnum.IronChest : InventoryItemEnum.HealthPotion;
        var character = CreateCharacter(CurrentCharacterId, CurrentUserId,
            new InventorySlot(itemType, 2));
        character.Health = 50;
        seed.Add(character);

        await seed.SaveChangesAsync();

        await using var delayed = new DelayedPotionContext(options);
        await using var retry = new ApplicationDbContext(options);
        var request = equipment ? CreateGearRequest() : CreatePotionRequest();
        var pending = new UpdateCharacterInventoryCommandHandler(delayed, new TestCurrentUserService())
            .Handle(request, CancellationToken.None);

        await delayed.SaveStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.False(pending.IsCompleted);
        Assert.Equal(50, (await retry.Characters.Where(x => x.Id == CurrentCharacterId).SingleAsync()).Health);

        retry.ChangeTracker.Clear();

        var retried = await new UpdateCharacterInventoryCommandHandler(retry, new TestCurrentUserService())
            .Handle(request, CancellationToken.None);

        delayed.ResumeSave.SetResult();

        var original = await pending;

        Assert.Equal(UpdateCharacterInventoryStatusEnum.Applied, original.Status);
        Assert.Equal(original.Status, retried.Status);

        retry.ChangeTracker.Clear();

        var stored = await retry.CharacterInventories
            .Include(x => x.Character)
            .Where(x => x.Id == CurrentCharacterId)
            .SingleAsync();

        Assert.Equal(equipment ? 50 : 70, stored.Character.Health);
        Assert.Equal(1, stored.Inventory.GetCount(itemType));
        if (equipment)
        {
            Assert.Equal(InventoryItemEnum.IronChest, stored.Character.ChestType);
            Assert.Equal(120, stored.Character.MaxHealth);
        }

        Assert.Single(await retry.CharacterInventoryHealthReceipts.ToArrayAsync());
    }

    [Fact]
    public async Task GearReceipt_ReplayPreservesLaterDamageAndStatsAndBindsFullPayload()
    {
        await using var context = CreateContext();
        var character = CreateCharacter(CurrentCharacterId, CurrentUserId,
            new InventorySlot(InventoryItemEnum.IronChest, 2));
        character.Health = 50;
        context.Add(character);

        await context.SaveChangesAsync();

        var request = CreateGearRequest();
        var handler = new UpdateCharacterInventoryCommandHandler(context, new TestCurrentUserService());

        await handler.Handle(request, CancellationToken.None);

        character.Health = 40;
        character.Strength = 99;

        await context.SaveChangesAsync();

        var replay = await handler.Handle(request, CancellationToken.None);

        Assert.Equal(UpdateCharacterInventoryStatusEnum.Applied, replay.Status);
        Assert.Equal(40, character.Health);
        Assert.Equal(99, character.Strength);
        Assert.Equal(1, character.CharacterInventory.Inventory.GetCount(InventoryItemEnum.IronChest));

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
            request with { CharacterUpdate = request.CharacterUpdate! with { Strength = 20 } }, CancellationToken.None));

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
            request with { Add = [new InventoryItemDto { Type = InventoryItemEnum.IronHelmet, Count = 1 }] }, CancellationToken.None));
    }

    private static UpdateCharacterInventoryCommand CreateGearRequest() => new(
        [], [new InventoryItemDto { Type = InventoryItemEnum.IronChest, Count = 1 }],
        CharacterUpdate: new UpdateCharacterCommand { Health = 50, MaxHealth = 120, Strength = 15, ChestType = InventoryItemEnum.IronChest },
        HealthOperationId: Guid.NewGuid());

    [Fact]
    public async Task AmmoReceipt_ReplayDoesNotOverwriteLaterEquipmentOrHealth()
    {
        await using var context = CreateContext();
        var character = CreateCharacter(CurrentCharacterId, CurrentUserId);
        character.AmmoType = InventoryItemEnum.AmmoOil1;
        character.AmmoCount = 1;
        character.Strength = 25;
        character.Health = 50;
        context.Add(character);

        await context.SaveChangesAsync();

        var request = new UpdateCharacterInventoryCommand([], [],
            CharacterUpdate: new UpdateCharacterCommand
            {
                AmmoType = InventoryItemEnum.AmmoTemplate,
                AmmoCount = 0,
                Strength = 20
            },
            HealthOperationId: Guid.NewGuid());
        var handler = new UpdateCharacterInventoryCommandHandler(context, new TestCurrentUserService());

        var applied = await handler.Handle(request, CancellationToken.None);

        Assert.Equal(UpdateCharacterInventoryStatusEnum.Applied, applied.Status);
        Assert.Equal(50, character.Health);
        Assert.Equal(20, character.Strength);
        Assert.Equal(0, character.AmmoCount);

        character.Health = 40;
        character.Strength = 35;
        character.AmmoType = InventoryItemEnum.AmmoOil3;
        character.AmmoCount = 8;

        await context.SaveChangesAsync();

        var replay = await handler.Handle(request, CancellationToken.None);

        Assert.Equal(UpdateCharacterInventoryStatusEnum.Applied, replay.Status);
        Assert.Equal(40, character.Health);
        Assert.Equal(35, character.Strength);
        Assert.Equal(InventoryItemEnum.AmmoOil3, character.AmmoType);
        Assert.Equal(8, character.AmmoCount);
        Assert.Single(await context.CharacterInventoryHealthReceipts.ToArrayAsync());

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
            request with { CharacterUpdate = request.CharacterUpdate! with { AmmoCount = 2 } }, CancellationToken.None));
    }

    [Fact]
    public async Task AmmoReceipt_OverlappingRetryDoesNotOverwriteSubsequentEquipment()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var seed = new ApplicationDbContext(options);
        var character = CreateCharacter(CurrentCharacterId, CurrentUserId);
        character.AmmoType = InventoryItemEnum.AmmoOil1;
        character.AmmoCount = 3;
        seed.Add(character);

        await seed.SaveChangesAsync();

        await using var delayed = new DelayedPotionContext(options);
        await using var retry = new ApplicationDbContext(options);
        var request = new UpdateCharacterInventoryCommand([], [],
            CharacterUpdate: new UpdateCharacterCommand { AmmoType = InventoryItemEnum.AmmoOil1, AmmoCount = 2 },
            HealthOperationId: Guid.NewGuid());
        var pending = new UpdateCharacterInventoryCommandHandler(delayed, new TestCurrentUserService())
            .Handle(request, CancellationToken.None);

        await delayed.SaveStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));

        var handler = new UpdateCharacterInventoryCommandHandler(retry, new TestCurrentUserService());
        var replay = await handler.Handle(request, CancellationToken.None);

        var stored = await retry.Characters
            .Where(x => x.Id == CurrentCharacterId)
            .SingleAsync();

        stored.AmmoType = InventoryItemEnum.AmmoOil3;
        stored.AmmoCount = 8;
        stored.Strength = 35;

        await retry.SaveChangesAsync();

        delayed.ResumeSave.SetResult();

        var original = await pending;

        retry.ChangeTracker.Clear();

        var final = await retry.Characters
            .Where(x => x.Id == CurrentCharacterId)
            .SingleAsync();

        Assert.Equal(UpdateCharacterInventoryStatusEnum.Applied, original.Status);
        Assert.Equal(original.Status, replay.Status);
        Assert.Equal(InventoryItemEnum.AmmoOil3, final.AmmoType);
        Assert.Equal(8, final.AmmoCount);
        Assert.Equal(35, final.Strength);
    }

    private static UpdateCharacterInventoryCommand CreatePotionRequest() => new(
        [], [new InventoryItemDto { Type = InventoryItemEnum.HealthPotion, Count = 1 }],
        CharacterUpdate: new UpdateCharacterCommand { Health = 70 }, HealthOperationId: Guid.NewGuid());

    private sealed class DelayedPotionContext(DbContextOptions<ApplicationDbContext> options) : ApplicationDbContext(options)
    {
        public TaskCompletionSource SaveStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource ResumeSave { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            SaveStarted.SetResult();

            await ResumeSave.Task.WaitAsync(cancellationToken);

            // Model the SQL revision/receipt conflict when an overlapping retry commits first.
            throw new DbUpdateConcurrencyException("The retry already committed this operation.");
        }
    }

    [Theory]
    [InlineData(1, true, 2, CharacterQuestStatusEnum.Finished)]
    [InlineData(2, false, 1, CharacterQuestStatusEnum.Accepted)]
    public async Task InventoryUpdate_PersistsCollectWithoutFollowUpNotification(
        int initialCount,
        bool adding,
        int expectedCount,
        CharacterQuestStatusEnum expectedStatus)
    {
        await using var context = CreateContext();
        var character = CreateCharacter(CurrentCharacterId, CurrentUserId,
            new InventorySlot(InventoryItemEnum.Can, initialCount));
        var otherCharacter = CreateCharacter(OtherOwnedCharacterId, CurrentUserId);
        var quest = CreateCollectQuest();
        var progress = CreateCharacterQuest(31, character, quest,
            adding ? CharacterQuestStatusEnum.Accepted : CharacterQuestStatusEnum.Finished);
        progress.Progress = initialCount;

        var otherProgress = CreateCharacterQuest(32, otherCharacter, quest, CharacterQuestStatusEnum.Accepted);
        var completed = CreateCharacterQuest(33, character, quest, CharacterQuestStatusEnum.Completed);
        completed.Progress = 2;

        var killQuest = new Quest
        {
            Id = QuestEnum.Kill2Beans,
            Name = nameof(QuestEnum.Kill2Beans),

        };
        var killProgress = CreateCharacterQuest(34, character, killQuest, CharacterQuestStatusEnum.Accepted);
        killProgress.Progress = 1;
        killProgress.ObjectiveProgress["kill:Bean(Clone)"] = 1;

        context.AddRange(character, otherCharacter, quest, progress, otherProgress, completed, killQuest, killProgress);

        await context.SaveChangesAsync();

        var saves = 0;
        context.SavingChanges += (_, _) => saves++;
        var item = new InventoryItemDto { Type = InventoryItemEnum.Can, Count = 1 };
        var request = new UpdateCharacterInventoryCommand(adding ? [item] : [], adding ? [] : [item]);

        var result = await new UpdateCharacterInventoryCommandHandler(context, new TestCurrentUserService())
            .Handle(request, CancellationToken.None);

        // Reload persisted state without CheckCharacterQuestProgress, as after a disconnect.
        context.ChangeTracker.Clear();

        var storedInventory = await context.CharacterInventories
            .Where(x => x.Id == CurrentCharacterId)
            .SingleAsync();

        var storedQuests = await context.CharacterQuests
            .ToDictionaryAsync(x => x.Id);

        Assert.Equal(UpdateCharacterInventoryStatusEnum.Applied, result.Status);
        Assert.Equal(1, saves);
        Assert.Equal(expectedCount, storedInventory.Inventory.GetCount(InventoryItemEnum.Can));
        Assert.Equal(expectedCount, storedQuests[31].Progress);
        Assert.Equal(expectedStatus, storedQuests[31].Status);
        Assert.Equal(0, storedQuests[32].Progress);
        Assert.Equal(CharacterQuestStatusEnum.Accepted, storedQuests[32].Status);
        Assert.Equal(2, storedQuests[33].Progress);
        Assert.Equal(CharacterQuestStatusEnum.Completed, storedQuests[33].Status);
        Assert.Equal(1, storedQuests[34].Progress);
        Assert.Equal(CharacterQuestStatusEnum.Accepted, storedQuests[34].Status);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InventoryUpdate_WhenRejected_PreservesPersistedCollectProgress(bool concurrencyConflict)
    {
        await using var context = CreateContext();
        var character = CreateCharacter(CurrentCharacterId, CurrentUserId,
            new InventorySlot(InventoryItemEnum.Can, 2));
        character.CharacterInventory.Count = 1;

        var quest = CreateCollectQuest();
        var progress = CreateCharacterQuest(31, character, quest, CharacterQuestStatusEnum.Finished);
        progress.Progress = 2;
        context.AddRange(character, quest, progress);

        await context.SaveChangesAsync();

        if (concurrencyConflict)
        {
            context.SavingChanges += (_, _) => throw new DbUpdateConcurrencyException("Simulated inventory conflict");
        }

        var request = new UpdateCharacterInventoryCommand(
            concurrencyConflict ? [] : [new InventoryItemDto { Type = InventoryItemEnum.Fish, Count = 1 }],
            [new InventoryItemDto { Type = InventoryItemEnum.Can, Count = 1 }]);

        var result = await new UpdateCharacterInventoryCommandHandler(context, new TestCurrentUserService())
            .Handle(request, CancellationToken.None);

        context.ChangeTracker.Clear();

        var storedInventory = await context.CharacterInventories
            .SingleAsync();

        var storedProgress = await context.CharacterQuests
            .SingleAsync();

        Assert.Equal(concurrencyConflict
            ? UpdateCharacterInventoryStatusEnum.InventoryChanged
            : UpdateCharacterInventoryStatusEnum.InventoryFull, result.Status);
        Assert.Equal(2, storedInventory.Inventory.GetCount(InventoryItemEnum.Can));
        Assert.Equal(0, storedInventory.Inventory.GetCount(InventoryItemEnum.Fish));
        Assert.Equal(2, storedProgress.Progress);
        Assert.Equal(CharacterQuestStatusEnum.Finished, storedProgress.Status);
    }

    [Fact]
    public async Task HealthPotion_PersistsHealingAndConsumptionTogether()
    {
        await using var context = CreateContext();
        var character = CreateCharacter(CurrentCharacterId, CurrentUserId,
            new InventorySlot(InventoryItemEnum.HealthPotion, 2));
        character.Health = 50;
        context.Characters.Add(character);

        await context.SaveChangesAsync();
        var saves = 0;
        context.SavingChanges += (_, _) => saves++;

        var result = await new UpdateCharacterInventoryCommandHandler(context, new TestCurrentUserService())
            .Handle(new([], [new InventoryItemDto { Type = InventoryItemEnum.HealthPotion, Count = 1 }],
                CharacterUpdate: new UpdateCharacterCommand { Health = 70 }), default);

        context.ChangeTracker.Clear();
        var stored = await context.Characters.Include(x => x.CharacterInventory).SingleAsync();

        Assert.Equal(UpdateCharacterInventoryStatusEnum.Applied, result.Status);
        Assert.Equal(1, saves);
        Assert.Equal(70, stored.Health);
        Assert.Equal(1, stored.CharacterInventory.Inventory.Items.Single().Count);
    }

    [Fact]
    public async Task HealthPotion_WhenInventoryConflicts_PersistsNeitherHealingNorConsumption()
    {
        await using var context = CreateContext();
        var character = CreateCharacter(CurrentCharacterId, CurrentUserId,
            new InventorySlot(InventoryItemEnum.HealthPotion, 2));
        character.Health = 50;
        context.Characters.Add(character);

        await context.SaveChangesAsync();

        context.SavingChanges += (_, _) => throw new DbUpdateConcurrencyException("Simulated concurrent loot save");

        var result = await new UpdateCharacterInventoryCommandHandler(context, new TestCurrentUserService())
            .Handle(new([], [new InventoryItemDto { Type = InventoryItemEnum.HealthPotion, Count = 1 }],
                CharacterUpdate: new UpdateCharacterCommand { Health = 70 }), default);

        context.ChangeTracker.Clear();
        var stored = await context.Characters.Include(x => x.CharacterInventory).SingleAsync();

        Assert.Equal(UpdateCharacterInventoryStatusEnum.InventoryChanged, result.Status);
        Assert.Equal(50, stored.Health);
        Assert.Equal(2, stored.CharacterInventory.Inventory.Items.Single().Count);
    }

    [Fact]
    public async Task Equip_PersistsInventoryAndCharacterInOneSave()
    {
        await using var context = CreateContext();
        var character = CreateCharacter(CurrentCharacterId, CurrentUserId,
            new InventorySlot(InventoryItemEnum.HelmetTemplate, 1));
        context.Characters.Add(character);

        await context.SaveChangesAsync();
        var saves = 0;
        context.SavingChanges += (_, _) => saves++;

        var result = await new UpdateCharacterInventoryCommandHandler(context, new TestCurrentUserService())
            .Handle(new([], [new InventoryItemDto { Type = InventoryItemEnum.HelmetTemplate, Count = 1 }],
                CharacterUpdate: new UpdateCharacterCommand { HelmetType = InventoryItemEnum.HelmetTemplate, MaxHealth = 150 }), default);

        context.ChangeTracker.Clear();
        var stored = await context.Characters.Include(x => x.CharacterInventory).SingleAsync();

        Assert.Equal(UpdateCharacterInventoryStatusEnum.Applied, result.Status);
        Assert.Equal(1, saves);
        Assert.Equal(InventoryItemEnum.HelmetTemplate, stored.HelmetType);
        Assert.Equal(150, stored.MaxHealth);
        Assert.Equal(0, stored.CharacterInventory.Inventory.Items[0].Count);
    }

    [Fact]
    public async Task Unequip_WhenInventoryFull_PreservesEquipmentAndStats()
    {
        await using var context = CreateContext();
        var character = CreateCharacter(CurrentCharacterId, CurrentUserId,
            new InventorySlot(InventoryItemEnum.Currency, 1024));
        character.CharacterInventory.Count = 1;
        character.HelmetType = InventoryItemEnum.HelmetTemplate;
        context.Characters.Add(character);

        await context.SaveChangesAsync();

        var result = await new UpdateCharacterInventoryCommandHandler(context, new TestCurrentUserService())
            .Handle(new([new InventoryItemDto { Type = InventoryItemEnum.HelmetTemplate, Count = 1 }], [],
                CharacterUpdate: new UpdateCharacterCommand { HelmetType = InventoryItemEnum.None, MaxHealth = 50 }), default);

        context.ChangeTracker.Clear();
        var stored = await context.Characters.Include(x => x.CharacterInventory).SingleAsync();

        Assert.Equal(UpdateCharacterInventoryStatusEnum.InventoryFull, result.Status);
        Assert.Equal(InventoryItemEnum.HelmetTemplate, stored.HelmetType);
        Assert.Equal(100, stored.MaxHealth);
        Assert.Equal(1024, stored.CharacterInventory.Inventory.Items[0].Count);
    }

    [Fact]
    public async Task Equip_WhenSaveFails_PersistsNeitherInventoryNorEquipment()
    {
        await using var context = CreateContext();
        var character = CreateCharacter(CurrentCharacterId, CurrentUserId,
            new InventorySlot(InventoryItemEnum.HelmetTemplate, 1));
        context.Characters.Add(character);

        await context.SaveChangesAsync();
        context.SavingChanges += (_, _) => throw new InvalidOperationException("Simulated save failure");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new UpdateCharacterInventoryCommandHandler(context, new TestCurrentUserService())
                .Handle(new([], [new InventoryItemDto { Type = InventoryItemEnum.HelmetTemplate, Count = 1 }],
                    CharacterUpdate: new UpdateCharacterCommand { HelmetType = InventoryItemEnum.HelmetTemplate, MaxHealth = 150 }), default));

        context.ChangeTracker.Clear();
        var stored = await context.Characters.Include(x => x.CharacterInventory).SingleAsync();

        Assert.Equal(InventoryItemEnum.None, stored.HelmetType);
        Assert.Equal(100, stored.MaxHealth);
        Assert.Equal(1, stored.CharacterInventory.Inventory.Items[0].Count);
    }

    [Fact]
    public async Task ClientQueries_DoNotReadAnotherUsersCharacterData()
    {
        await using var context = CreateContext();

        context.Characters.AddRange(
            CreateCharacter(CurrentCharacterId, CurrentUserId),
            CreateCharacter(ForeignCharacterId, "other-user"));

        await context.SaveChangesAsync();

        var currentUser = new TestCurrentUserService();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new GetCharacterInventoryQueryHandler(context, currentUser)
                .Handle(new GetCharacterInventoryQuery(ForeignCharacterId), CancellationToken.None));

        var currentCharacter = await new GetCharacterQueryHandler(context, currentUser)
            .Handle(new GetCharacterQuery(), CancellationToken.None);

        Assert.Equal(CurrentCharacterId, currentCharacter.Id);
        Assert.Equal("current-user-character", currentCharacter.Name);
        Assert.Equal(100, currentCharacter.Health);
    }

    [Fact]
    public async Task CharacterHandlers_ReadAndModifyOwnedCharacter()
    {
        await using var context = CreateContext();

        context.Characters.AddRange(
            CreateCharacter(CurrentCharacterId, CurrentUserId),
            CreateCharacter(ForeignCharacterId, "other-user"));

        await context.SaveChangesAsync();

        var currentUser = new TestCurrentUserService();

        await new UpdateCharacterCommandHandler(context, currentUser)
            .Handle(
                new UpdateCharacterCommand
                {
                    Health = 75,
                    Strength = 9
                },
                CancellationToken.None);

        var experience = await new AddCharacterExperienceCommandHandler(context, currentUser)
            .Handle(
                new AddCharacterExperienceCommand
                {
                    Amount = 100,
                    Type = ExperienceTypeEnum.Cooking
                },
                CancellationToken.None);

        var character = await new GetCharacterQueryHandler(context, currentUser)
            .Handle(new GetCharacterQuery(), CancellationToken.None);

        Assert.Equal(CurrentCharacterId, character.Id);
        Assert.Equal(75, character.Health);
        Assert.Equal(9, character.Strength);
        Assert.Equal(100, experience.Experience);
        Assert.Equal(2, experience.Level);
        Assert.Equal(2, character.Levels[ExperienceTypeEnum.Cooking]);

        var foreignCharacter = await context.Characters
            .Include(x => x.CharacterExperiences)
            .Where(x => x.Id == ForeignCharacterId)
            .SingleAsync();

        Assert.Equal(100, foreignCharacter.Health);
        Assert.Empty(foreignCharacter.CharacterExperiences);
    }

    [Fact]
    public async Task InventoryHandlers_ReadAndModifyOwnedInventory()
    {
        await using var context = CreateContext();

        context.Characters.AddRange(
            CreateCharacter(
                CurrentCharacterId,
                CurrentUserId,
                new InventorySlot(InventoryItemEnum.HealthPotion, 2),
                new InventorySlot(InventoryItemEnum.Currency, 10)),
            CreateCharacter(
                ForeignCharacterId,
                "other-user",
                new InventorySlot(InventoryItemEnum.Fish, 7)));

        await context.SaveChangesAsync();

        var currentUser = new TestCurrentUserService();

        var update = await new UpdateCharacterInventoryCommandHandler(context, currentUser)
            .Handle(
                new UpdateCharacterInventoryCommand(
                    [new InventoryItemDto { Type = InventoryItemEnum.HealthPotion, Count = 3 }],
                    [new InventoryItemDto { Type = InventoryItemEnum.Currency, Count = 4 }]),
                CancellationToken.None);

        context.ChangeTracker.Clear();

        var inventory = await new GetCharacterInventoryQueryHandler(context, currentUser)
            .Handle(new GetCharacterInventoryQuery(CurrentCharacterId), CancellationToken.None);

        var foreignInventory = await context.CharacterInventories
            .Where(x => x.Id == ForeignCharacterId)
            .SingleAsync();

        Assert.Collection(
            inventory.Inventory.Items,
            x => Assert.Equal((InventoryItemEnum.HealthPotion, 5), (x.Type, x.Count)),
            x => Assert.Equal((InventoryItemEnum.Currency, 6), (x.Type, x.Count)));

        Assert.Equal(UpdateCharacterInventoryStatusEnum.Applied, update.Status);
        Assert.Equal((InventoryItemEnum.Fish, 7), (foreignInventory.Inventory.Items.Single().Type, foreignInventory.Inventory.Items.Single().Count));
    }

    [Fact]
    public async Task UpdateInventory_SplitsAddedItemsAtTheMaximumStackSize()
    {
        await using var context = CreateContext();

        var character = CreateCharacter(CurrentCharacterId, CurrentUserId);
        character.CharacterInventory.Count = 3;
        context.Characters.Add(character);

        await context.SaveChangesAsync();

        var result = await new UpdateCharacterInventoryCommandHandler(context, new TestCurrentUserService())
            .Handle(
                new UpdateCharacterInventoryCommand(
                    [new InventoryItemDto { Type = InventoryItemEnum.HealthPotion, Count = 2500 }],
                    []),
                CancellationToken.None);

        Assert.Equal(UpdateCharacterInventoryStatusEnum.Applied, result.Status);

        Assert.Collection(
            character.CharacterInventory.Inventory.Items,
            x => Assert.Equal((InventoryItemEnum.HealthPotion, 1024), (x.Type, x.Count)),
            x => Assert.Equal((InventoryItemEnum.HealthPotion, 1024), (x.Type, x.Count)),
            x => Assert.Equal((InventoryItemEnum.HealthPotion, 452), (x.Type, x.Count)));
    }

    [Fact]
    public async Task InventoryHandlers_ExposeAndPersistCapacityRequiredByNormalizedLegacySlots()
    {
        await using var context = CreateContext();

        var character = CreateCharacter(
            CurrentCharacterId,
            CurrentUserId,
            new InventorySlot(InventoryItemEnum.HealthPotion, 10),
            new InventorySlot(InventoryItemEnum.Currency, 10),
            new InventorySlot(InventoryItemEnum.Fish, 10));

        character.CharacterInventory.Count = 2;
        context.Characters.Add(character);

        await context.SaveChangesAsync();

        var currentUser = new TestCurrentUserService();
        var inventory = await new GetCharacterInventoryQueryHandler(context, currentUser)
            .Handle(new GetCharacterInventoryQuery(CurrentCharacterId), CancellationToken.None);

        var result = await new UpdateCharacterInventoryCommandHandler(context, currentUser)
            .Handle(
                new UpdateCharacterInventoryCommand(
                    [],
                    [new InventoryItemDto { Type = InventoryItemEnum.Fish, Count = 1 }]),
                CancellationToken.None);

        Assert.Equal(3, inventory.Count);
        Assert.Equal(UpdateCharacterInventoryStatusEnum.Applied, result.Status);
        Assert.Equal(3, character.CharacterInventory.Count);
        Assert.Equal(9, character.CharacterInventory.Inventory.Items[2].Count);
    }

    [Fact]
    public async Task UpdateInventory_RejectsTheWholeTransactionWhenInventoryIsFull()
    {
        await using var context = CreateContext();

        var character = CreateCharacter(
            CurrentCharacterId,
            CurrentUserId,
            new InventorySlot(InventoryItemEnum.HealthPotion, 1000),
            new InventorySlot(InventoryItemEnum.Currency, 1024));

        character.CharacterInventory.Count = 2;
        context.Characters.Add(character);

        await context.SaveChangesAsync();

        var result = await new UpdateCharacterInventoryCommandHandler(context, new TestCurrentUserService())
            .Handle(
                new UpdateCharacterInventoryCommand(
                    [new InventoryItemDto { Type = InventoryItemEnum.HealthPotion, Count = 25 }],
                    [new InventoryItemDto { Type = InventoryItemEnum.Currency, Count = 1 }]),
                CancellationToken.None);

        Assert.Equal(UpdateCharacterInventoryStatusEnum.InventoryFull, result.Status);
        Assert.Equal(1000, character.CharacterInventory.Inventory.Items[0].Count);
        Assert.Equal(1024, character.CharacterInventory.Inventory.Items[1].Count);
    }

    [Fact]
    public async Task QuestHandlers_ReadAndModifyOnlyOwnedQuests()
    {
        await using var context = CreateContext();

        var currentCharacter = CreateCharacter(CurrentCharacterId, CurrentUserId);
        var foreignCharacter = CreateCharacter(ForeignCharacterId, "other-user");

        var quest = new Quest
        {
            Id = QuestEnum.Kill2Beans,
            Name = nameof(QuestEnum.Kill2Beans),

            Status = StatusEnum.Active
        };

        var currentQuest = CreateCharacterQuest(10, currentCharacter, quest, CharacterQuestStatusEnum.Accepted);
        var foreignAcceptedQuest = CreateCharacterQuest(20, foreignCharacter, quest, CharacterQuestStatusEnum.Accepted);
        var foreignFinishedQuest = CreateCharacterQuest(21, foreignCharacter, quest, CharacterQuestStatusEnum.Finished);

        context.AddRange(currentCharacter, foreignCharacter, quest, currentQuest, foreignAcceptedQuest, foreignFinishedQuest);

        await context.SaveChangesAsync();

        var currentUser = new TestCurrentUserService();

        var initialQuests = await new GetCharacterQuestsHandler(context, currentUser)
            .Handle(new GetCharacterQuestsQuery(CurrentCharacterId), CancellationToken.None);

        var progress = await new CheckCharacterQuestProgressCommandHandler(context, currentUser)
            .Handle(
                new CheckCharacterQuestProgressCommand(QuestEnum.Kill2Beans, 2),
                CancellationToken.None);

        var completedAtUtc = new DateTimeOffset(2026, 8, 12, 12, 0, 0, TimeSpan.Zero);

        var completion = await new CompleteCharacterQuestCommandHandler(context, currentUser, new FixedTimeProvider(completedAtUtc))
            .Handle(new CompleteCharacterQuestCommand(currentQuest.Id), CancellationToken.None);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new CompleteCharacterQuestCommandHandler(context, currentUser, new FixedTimeProvider(completedAtUtc))
                .Handle(new CompleteCharacterQuestCommand(foreignFinishedQuest.Id), CancellationToken.None));

        Assert.Collection(initialQuests.CharacterQuests, x => Assert.Equal(currentQuest.Id, x.Id));
        Assert.Equal(CharacterQuestStatusEnum.Finished, progress.Status);
        Assert.Equal(CompleteCharacterQuestStatusEnum.Applied, completion.Status);
        Assert.Equal(QuestEnum.Kill2Beans, completion.QuestId);
        Assert.Equal(1000, completion.Reward);
        Assert.Equal(3, completion.Level);
        Assert.Equal(1000, currentCharacter.CharacterExperiences
            .Where(x => x.Type == ExperienceTypeEnum.Main)
            .Sum(x => x.Amount));
        Assert.Equal(CharacterQuestStatusEnum.Completed, currentQuest.Status);
        Assert.Equal(completedAtUtc, currentQuest.EndDate);
        Assert.Equal(CharacterQuestStatusEnum.Accepted, foreignAcceptedQuest.Status);
        Assert.Equal(0, foreignAcceptedQuest.Progress);
        Assert.Equal(CharacterQuestStatusEnum.Finished, foreignFinishedQuest.Status);
    }

    [Fact]
    public async Task CompleteCollectQuest_RemovesRequiredItemsExactlyOnce()
    {
        await using var context = CreateContext();

        var character = CreateCharacter(
            CurrentCharacterId,
            CurrentUserId,
            new InventorySlot(InventoryItemEnum.Can, 3));

        var quest = new Quest
        {
            Id = QuestEnum.Collect2Cans,
            Name = nameof(QuestEnum.Collect2Cans),

            Status = StatusEnum.Active
        };

        var characterQuest = CreateCharacterQuest(
            30,
            character,
            quest,
            CharacterQuestStatusEnum.Finished);

        context.AddRange(character, quest, characterQuest);

        await context.SaveChangesAsync();

        var completedAtUtc = new DateTimeOffset(2026, 8, 12, 12, 0, 0, TimeSpan.Zero);

        var result = await new CompleteCharacterQuestCommandHandler(
                context,
                new TestCurrentUserService(),
                new FixedTimeProvider(completedAtUtc))
            .Handle(new CompleteCharacterQuestCommand(characterQuest.Id), CancellationToken.None);

        var remainingItem = character.CharacterInventory.Inventory.Items
            .Where(x => x.Type == InventoryItemEnum.Can)
            .Single();

        Assert.Equal(15, character.CharacterInventory.Inventory.GetCount(InventoryItemEnum.Currency));

        Assert.Equal(CompleteCharacterQuestStatusEnum.Applied, result.Status);
        Assert.Equal(QuestEnum.Collect2Cans, result.QuestId);
        Assert.Equal(1000, result.Reward);
        Assert.Equal(3, result.Level);
        Assert.Equal(1000, character.CharacterExperiences
            .Where(x => x.Type == ExperienceTypeEnum.Main)
            .Sum(x => x.Amount));
        Assert.Equal(CharacterQuestStatusEnum.Completed, characterQuest.Status);
        Assert.Equal((InventoryItemEnum.Can, 1), (remainingItem.Type, remainingItem.Count));
    }

    [Theory]
    [InlineData(2, 1, CompleteCharacterQuestStatusEnum.Applied)]
    [InlineData(3, 1, CompleteCharacterQuestStatusEnum.InventoryFull)]
    [InlineData(1, 2, CompleteCharacterQuestStatusEnum.InventoryChanged)]
    public async Task CompleteCollectQuest_UsesFreedSlotsAndRejectsWithoutPartialChanges(
        int cans, short capacity, CompleteCharacterQuestStatusEnum expected)
    {
        await using var context = CreateContext();
        var character = CreateCharacter(CurrentCharacterId, CurrentUserId, new InventorySlot(InventoryItemEnum.Can, cans));
        character.CharacterInventory.Count = capacity;
        var quest = CreateCollectQuest();
        var characterQuest = CreateCharacterQuest(31, character, quest, CharacterQuestStatusEnum.Finished);

        context.AddRange(character, quest, characterQuest);
        await context.SaveChangesAsync();

        var handler = new CompleteCharacterQuestCommandHandler(context, new TestCurrentUserService(), TimeProvider.System);
        var result = await handler.Handle(new CompleteCharacterQuestCommand(characterQuest.Id), CancellationToken.None);

        Assert.Equal(expected, result.Status);

        if (expected == CompleteCharacterQuestStatusEnum.Applied)
        {
            Assert.Equal(0, character.CharacterInventory.Inventory.GetCount(InventoryItemEnum.Can));
            Assert.Equal(15, character.CharacterInventory.Inventory.GetCount(InventoryItemEnum.Currency));
            Assert.Equal(CharacterQuestStatusEnum.Completed, characterQuest.Status);
            await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
                new CompleteCharacterQuestCommand(characterQuest.Id), CancellationToken.None));
        }
        else
        {
            Assert.Equal(cans, character.CharacterInventory.Inventory.GetCount(InventoryItemEnum.Can));
            Assert.Equal(0, character.CharacterInventory.Inventory.GetCount(InventoryItemEnum.Currency));
            Assert.Empty(character.CharacterExperiences);
            Assert.Equal(CharacterQuestStatusEnum.Finished, characterQuest.Status);
        }
    }

    [Fact]
    public async Task CompleteCollectQuest_UsesCapacityExpandedByLegacyNormalization()
    {
        await using var context = CreateContext();
        var converter = context.Model.FindEntityType(typeof(CharacterInventory))!
            .FindProperty(nameof(CharacterInventory.Inventory))!.GetValueConverter()!;
        var legacyJson = $$"""{"Items":[{"Type":{{(int)InventoryItemEnum.Currency}},"Count":2500},{"Type":{{(int)InventoryItemEnum.Can}},"Count":2}]}""";
        var inventory = Assert.IsType<InventoryState>(converter.ConvertFromProvider(legacyJson));
        var character = CreateCharacter(CurrentCharacterId, CurrentUserId);
        character.CharacterInventory.Count = 2;
        character.CharacterInventory.Inventory = inventory;

        var quest = CreateCollectQuest();
        var characterQuest = CreateCharacterQuest(31, character, quest, CharacterQuestStatusEnum.Finished);

        context.AddRange(character, quest, characterQuest);
        await context.SaveChangesAsync();

        Assert.True(inventory.Items.Count > character.CharacterInventory.Count);

        var result = await new CompleteCharacterQuestCommandHandler(context, new TestCurrentUserService(), TimeProvider.System)
            .Handle(new CompleteCharacterQuestCommand(characterQuest.Id), CancellationToken.None);

        Assert.Equal(CompleteCharacterQuestStatusEnum.Applied, result.Status);

        context.ChangeTracker.Clear();

        var storedInventory = await context.CharacterInventories.SingleAsync();
        var storedQuest = await context.CharacterQuests.SingleAsync();

        Assert.Equal(2515, storedInventory.Inventory.GetCount(InventoryItemEnum.Currency));
        Assert.Equal(0, storedInventory.Inventory.GetCount(InventoryItemEnum.Can));
        Assert.Equal(inventory.Items.Count, storedInventory.Inventory.Items.Count);
        Assert.Equal(CharacterQuestStatusEnum.Completed, storedQuest.Status);
        Assert.Equal(1000, (await context.CharacterExperiences.SingleAsync()).Amount);
    }

    [Theory]
    [InlineData(1, CompleteCharacterQuestStatusEnum.InventoryFull)]
    [InlineData(2, CompleteCharacterQuestStatusEnum.Applied)]
    public async Task CompleteKillQuest_GrantsAllRewardsAndSynchronizesCollectProgress(
        short capacity, CompleteCharacterQuestStatusEnum expected)
    {
        await using var context = CreateContext();
        var character = CreateCharacter(CurrentCharacterId, CurrentUserId);
        character.CharacterInventory.Count = capacity;
        var quest = new Quest
        {
            Id = QuestEnum.Kill2Beans,
            Name = nameof(QuestEnum.Kill2Beans),

        };
        var collect = new Quest
        {
            Id = QuestEnum.Collect2HealthPotions,
            Name = nameof(QuestEnum.Collect2HealthPotions),

        };
        var finished = CreateCharacterQuest(40, character, quest, CharacterQuestStatusEnum.Finished);
        var collecting = CreateCharacterQuest(41, character, collect, CharacterQuestStatusEnum.Accepted);

        context.AddRange(character, quest, collect, finished, collecting);
        await context.SaveChangesAsync();

        var result = await new CompleteCharacterQuestCommandHandler(context, new TestCurrentUserService(), TimeProvider.System)
            .Handle(new CompleteCharacterQuestCommand(finished.Id), CancellationToken.None);

        Assert.Equal(expected, result.Status);

        if (expected == CompleteCharacterQuestStatusEnum.Applied)
        {
            Assert.Equal(25, character.CharacterInventory.Inventory.GetCount(InventoryItemEnum.Currency));
            Assert.Equal(2, character.CharacterInventory.Inventory.GetCount(InventoryItemEnum.HealthPotion));
            Assert.Equal(CharacterQuestStatusEnum.Finished, collecting.Status);
            Assert.Equal(2, Assert.Single(result.CollectProgress).Progress);
            Assert.Equal(1000, Assert.Single(character.CharacterExperiences).Amount);
        }
        else
        {
            Assert.Empty(character.CharacterInventory.Inventory.Items);
            Assert.Empty(character.CharacterExperiences);
            Assert.Equal(CharacterQuestStatusEnum.Finished, finished.Status);
            Assert.Equal(CharacterQuestStatusEnum.Accepted, collecting.Status);
        }
    }

    [Fact]
    public async Task AcceptCollectQuest_UsesCurrentInventoryAsInitialProgress()
    {
        await using var context = CreateContext();

        var character = CreateCharacter(
            CurrentCharacterId,
            CurrentUserId,
            new InventorySlot(InventoryItemEnum.Can, 2));

        var quest = CreateCollectQuest();

        context.AddRange(character, quest);

        await context.SaveChangesAsync();

        var result = await new AcceptCharacterQuestCommandHandler(
                context,
                new TestCurrentUserService(),
                new FixedTimeProvider(new DateTimeOffset(2026, 8, 12, 12, 0, 0, TimeSpan.Zero)))
            .Handle(new AcceptCharacterQuestCommand(QuestEnum.Collect2Cans), CancellationToken.None);

        Assert.Equal(2, result.Progress);
        Assert.Equal(CharacterQuestStatusEnum.Finished, result.Status);
    }

    [Fact]
    public async Task CheckCollectQuestProgress_TracksCurrentInventoryAndReopensAfterItemLoss()
    {
        await using var context = CreateContext();

        var character = CreateCharacter(
            CurrentCharacterId,
            CurrentUserId,
            new InventorySlot(InventoryItemEnum.Can, 2));

        var quest = CreateCollectQuest();
        var characterQuest = CreateCharacterQuest(31, character, quest, CharacterQuestStatusEnum.Accepted);

        context.AddRange(character, quest, characterQuest);

        await context.SaveChangesAsync();

        var handler = new CheckCharacterQuestProgressCommandHandler(context, new TestCurrentUserService());

        var finished = await handler.Handle(
            new CheckCharacterQuestProgressCommand(QuestEnum.Collect2Cans, 0),
            CancellationToken.None);

        character.CharacterInventory.Inventory.Remove(InventoryItemEnum.Can, 1);

        await context.SaveChangesAsync();

        var reopened = await handler.Handle(
            new CheckCharacterQuestProgressCommand(QuestEnum.Collect2Cans, 0),
            CancellationToken.None);

        Assert.Equal(2, finished.Progress);
        Assert.Equal(CharacterQuestStatusEnum.Finished, finished.Status);
        Assert.Equal(1, reopened.Progress);
        Assert.Equal(CharacterQuestStatusEnum.Accepted, reopened.Status);
    }

    [Fact]
    public async Task GetCharacters_ReturnsAllActiveCharactersOwnedByUser()
    {
        await using var context = CreateContext();

        var removedCharacter = CreateCharacter(44, CurrentUserId);
        removedCharacter.Status = StatusEnum.Removed;

        context.Characters.AddRange(
            CreateCharacter(CurrentCharacterId, CurrentUserId),
            CreateCharacter(OtherOwnedCharacterId, CurrentUserId),
            removedCharacter,
            CreateCharacter(ForeignCharacterId, "other-user"));

        await context.SaveChangesAsync();

        var result = await new GetCharactersQueryHandler(context, new TestCurrentUserService())
            .Handle(new GetCharactersQuery(), CancellationToken.None);

        Assert.Collection(
            result.Characters,
            x => Assert.Equal(CurrentCharacterId, x.Id),
            x => Assert.Equal(OtherOwnedCharacterId, x.Id));
    }

    [Fact]
    public async Task ServerHandlers_UseCharacterSelectedByPlayerSession()
    {
        await using var context = CreateContext();

        var currentCharacter = CreateCharacter(CurrentCharacterId, CurrentUserId);
        var otherOwnedCharacter = CreateCharacter(OtherOwnedCharacterId, CurrentUserId);
        var quest = new Quest
        {
            Id = QuestEnum.Kill2Beans,
            Name = nameof(QuestEnum.Kill2Beans),

            Status = StatusEnum.Active
        };

        context.AddRange(currentCharacter, otherOwnedCharacter, quest);

        await context.SaveChangesAsync();

        var currentUser = new TestCurrentUserService();

        await new UpdateCharacterCommandHandler(context, currentUser)
            .Handle(
                new UpdateCharacterCommand { Health = 75 },
                CancellationToken.None);

        await new UpdateCharacterInventoryCommandHandler(context, currentUser)
            .Handle(
                new UpdateCharacterInventoryCommand(
                    [new InventoryItemDto { Type = InventoryItemEnum.HealthPotion, Count = 1 }],
                    []),
                CancellationToken.None);

        var acceptedQuest = await new AcceptCharacterQuestCommandHandler(
                context,
                currentUser,
                new FixedTimeProvider(new DateTimeOffset(2026, 8, 12, 12, 0, 0, TimeSpan.Zero)))
            .Handle(
                new AcceptCharacterQuestCommand(QuestEnum.Kill2Beans),
                CancellationToken.None);

        var selectedCharacter = await new GetCharacterQueryHandler(context, currentUser)
            .Handle(new GetCharacterQuery(), CancellationToken.None);

        var persistedQuest = await context.CharacterQuests
            .Where(x => x.Id == acceptedQuest.Id)
            .SingleAsync();

        Assert.Equal(CurrentCharacterId, selectedCharacter.Id);
        Assert.Equal(75, currentCharacter.Health);
        Assert.Equal(100, otherOwnedCharacter.Health);
        Assert.Single(currentCharacter.CharacterInventory.Inventory.Items);
        Assert.Empty(otherOwnedCharacter.CharacterInventory.Inventory.Items);
        Assert.Equal(CurrentCharacterId, persistedQuest.CharacterId);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static Character CreateCharacter(int id, string userId, params InventorySlot[] inventory)
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
                Count = 15
            }
        };
    }

    private static CharacterQuest CreateCharacterQuest(
        int id,
        Character character,
        Quest quest,
        CharacterQuestStatusEnum status)
    {
        return new CharacterQuest
        {
            Id = id,
            CharacterId = character.Id,
            Character = character,
            QuestId = quest.Id,
            Quest = quest,
            ObjectiveProgress = status == CharacterQuestStatusEnum.Finished
                ? quest.Id.GetRequirements().ToDictionary(x => x.Id, x => x.Count) : [],
            Status = status,
            StartDate = new DateTimeOffset(2026, 8, 12, 10, 0, 0, TimeSpan.Zero)
        };
    }

    private static Quest CreateCollectQuest()
    {
        return new Quest
        {
            Id = QuestEnum.Collect2Cans,
            Name = nameof(QuestEnum.Collect2Cans),

            Status = StatusEnum.Active
        };
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public LanguageEnum Language => LanguageEnum.en;

        public List<string>? Roles => [];

        public string GetId()
        {
            return CurrentUserId;
        }

        public string GetAuthenticatedUserId()
        {
            return CurrentUserId;
        }

        public int? GetCharacterId()
        {
            return CurrentCharacterId;
        }

        public DateTimeOffset? GetAuthenticatedSessionStartedAtUtc()
        {
            return null;
        }

        public DateTimeOffset? GetAuthenticatedTokenExpirationUtc()
        {
            return null;
        }
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FixedTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }
    }
}
