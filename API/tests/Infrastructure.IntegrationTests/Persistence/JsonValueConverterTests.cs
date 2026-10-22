using Microsoft.EntityFrameworkCore;
using ProjectX.Domain.Crafting;
using ProjectX.Domain.Entities;
using ProjectX.Domain.Enums;
using ProjectX.Domain.Hideouts;
using ProjectX.Domain.Inventory;
using ProjectX.Infrastructure.Persistance;

namespace ProjectX.Infrastructure.IntegrationTests.Persistence;

public class JsonValueConverterTests
{
    [Fact]
    public async Task CharacterInventory_NormalizedLegacyStateSavesWithIndependentRevision()
    {
        await using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(CharacterInventory))!;
        var inventoryProperty = entityType.FindProperty(nameof(CharacterInventory.Inventory))!;
        var converter = inventoryProperty.GetValueConverter()!;
        const string legacyJson = """{"Items":[{"Type":101,"Count":2500},{"Type":0,"Count":0}]}""";
        var inventory = Assert.IsType<InventoryState>(converter.ConvertFromProvider(legacyJson));

        Assert.False(inventoryProperty.IsConcurrencyToken);
        Assert.True(entityType.FindProperty(nameof(CharacterInventory.Revision))!.IsConcurrencyToken);
        Assert.NotEqual(legacyJson, converter.ConvertToProvider(inventory));

        context.CharacterInventories.Add(new CharacterInventory { Id = 42, Count = 15, Inventory = inventory });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var stored = await context.CharacterInventories.SingleAsync();
        stored.Inventory.Add(InventoryItemEnum.Currency, 1, capacity: 15);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var updated = await context.CharacterInventories.SingleAsync();

        Assert.Equal(2501, updated.Inventory.Items.Sum(x => x.Count));
        Assert.Equal(1, updated.Revision);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CharacterInventory_RejectsStaleWriter(bool synchronousSave)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var firstContext = new ApplicationDbContext(options);
        firstContext.CharacterInventories.Add(new CharacterInventory
        {
            Id = 42,
            Count = 15,
            Inventory = new InventoryState([new InventorySlot(InventoryItemEnum.Currency, 10)])
        });

        await firstContext.SaveChangesAsync();
        await using var secondContext = new ApplicationDbContext(options);
        var first = await firstContext.CharacterInventories.SingleAsync();
        var second = await secondContext.CharacterInventories.SingleAsync();
        first.Inventory.Add(InventoryItemEnum.Currency, 1, capacity: 15);
        second.Inventory.Add(InventoryItemEnum.Currency, 2, capacity: 15);

        if (synchronousSave)
        {
            firstContext.SaveChanges();

            Assert.Throws<DbUpdateConcurrencyException>(() => secondContext.SaveChanges());
        }
        else
        {
            await firstContext.SaveChangesAsync();

            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => secondContext.SaveChangesAsync());
        }

        secondContext.ChangeTracker.Clear();
        var persisted = await secondContext.CharacterInventories.SingleAsync();

        Assert.Equal(11, persisted.Inventory.Items.Single().Count);
        Assert.Equal(1, persisted.Revision);
    }

    [Fact]
    public void CharacterInventoryConverter_RoundTripsDomainStateAndTracksMutations()
    {
        using var context = CreateContext();
        var property = context.Model.FindEntityType(typeof(CharacterInventory))!
            .FindProperty(nameof(CharacterInventory.Inventory))!;
        var converter = property.GetValueConverter()!;
        var comparer = property.GetValueComparer()!;
        var inventory = new InventoryState(
        [
            new InventorySlot(InventoryItemEnum.HealthPotion, 4),
            InventorySlot.Empty()
        ]);

        var providerValue = converter.ConvertToProvider(inventory);
        var restored = Assert.IsType<InventoryState>(converter.ConvertFromProvider(providerValue));
        var snapshot = Assert.IsType<InventoryState>(comparer.Snapshot(inventory));

        Assert.Equal(
            inventory.Items.Select(slot => (slot.Type, slot.Count)),
            restored.Items.Select(slot => (slot.Type, slot.Count)));

        inventory.Add(InventoryItemEnum.Currency, 10, capacity: 15);

        Assert.False(comparer.Equals(inventory, snapshot));
    }

    [Fact]
    public void CharacterInventoryConverter_ReadsLegacyObjectAndTemporaryArrayFormats()
    {
        using var context = CreateContext();
        var converter = context.Model.FindEntityType(typeof(CharacterInventory))!
            .FindProperty(nameof(CharacterInventory.Inventory))!
            .GetValueConverter()!;
        const string legacyObject = """{"Items":[{"Type":501,"Count":4}]}""";
        const string temporaryArray = """[{"type":501,"count":4}]""";

        var restoredLegacy = Assert.IsType<InventoryState>(converter.ConvertFromProvider(legacyObject));
        var restoredTemporary = Assert.IsType<InventoryState>(converter.ConvertFromProvider(temporaryArray));

        Assert.Equal((InventoryItemEnum.HealthPotion, 4), (restoredLegacy.Items.Single().Type, restoredLegacy.Items.Single().Count));
        Assert.Equal((InventoryItemEnum.HealthPotion, 4), (restoredTemporary.Items.Single().Type, restoredTemporary.Items.Single().Count));
    }

    [Fact]
    public void CharacterInventoryConverter_SplitsLegacyOversizedStacksAndUsesEmptySlotsFirst()
    {
        using var context = CreateContext();
        var converter = context.Model.FindEntityType(typeof(CharacterInventory))!
            .FindProperty(nameof(CharacterInventory.Inventory))!
            .GetValueConverter()!;
        const string legacyInventory = """{"items":[{"type":101,"count":2500},{"type":0,"count":0}]}""";

        var restored = Assert.IsType<InventoryState>(converter.ConvertFromProvider(legacyInventory));

        Assert.Collection(
            restored.Items,
            x => Assert.Equal((InventoryItemEnum.Currency, 1024), (x.Type, x.Count)),
            x => Assert.Equal((InventoryItemEnum.Currency, 1024), (x.Type, x.Count)),
            x => Assert.Equal((InventoryItemEnum.Currency, 452), (x.Type, x.Count)));
    }

    [Fact]
    public void CraftingRecipeConverters_RoundTripTypedDefinition()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(CraftingRecipe))!;
        var definition = CraftingRecipeEnum.Sushi.GetDefinition();

        var requirementConverter = entityType
            .FindProperty(nameof(CraftingRecipe.Requirement))!
            .GetValueConverter()!;
        var rewardConverter = entityType
            .FindProperty(nameof(CraftingRecipe.Reward))!
            .GetValueConverter()!;

        var requirement = Assert.IsType<CraftingRecipeRequirement>(requirementConverter.ConvertFromProvider(
            requirementConverter.ConvertToProvider(definition.Requirement)));
        var reward = Assert.IsType<CraftingRecipeReward>(rewardConverter.ConvertFromProvider(
            rewardConverter.ConvertToProvider(definition.Reward)));

        Assert.Equal(definition.Requirement.Level, requirement.Level);
        Assert.Equal(definition.Requirement.Items, requirement.Items);
        Assert.Equal(definition.Reward, reward);
    }

    [Fact]
    public async Task HideoutJson_PreservesSlotsAndDeadlines_WithSeparateConcurrencyRevision()
    {
        await using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(CharacterHideout))!;
        var property = entity.FindProperty(nameof(CharacterHideout.Data))!;
        Assert.Null(property.GetValueConverter());
        var data = new HideoutStateSerializer().Deserialize<FarmState>("{}");
        var deadline = new DateTimeOffset(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);
        data.UpgradeEndsAt = deadline;
        data.Slots[1].Seeds = new InventorySlot(InventoryItemEnum.RaspberrySeed, 1024);
        data.Slots[1].ReadyAt = deadline;

        var restored = new HideoutStateSerializer().Deserialize<FarmState>(new HideoutStateSerializer().Serialize(data));

        Assert.False(property.IsConcurrencyToken);
        Assert.True(entity.FindProperty(nameof(CharacterHideout.Revision))!.IsConcurrencyToken);
        Assert.Equal(6, restored.Slots.Length);
        Assert.Equal(InventoryItemEnum.None, restored.Slots[0].Seeds.Type);
        Assert.Equal(1024, restored.Slots[1].Seeds.Count);
        Assert.Equal(deadline, restored.Slots[1].ReadyAt);
        Assert.Equal(deadline, restored.UpgradeEndsAt);

        context.CharacterHideouts.Add(new CharacterHideout { CharacterId = 42, HideoutBuildingTypeId = HideoutBuildingEnum.Farm, Data = new HideoutStateSerializer().Serialize(restored) });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var row = await context.CharacterHideouts.SingleAsync();
        var farm = new HideoutStateSerializer().Deserialize<FarmState>(row.Data);
        farm.Advance(deadline);
        row.Data = new HideoutStateSerializer().Serialize(farm);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var saved = await context.CharacterHideouts.SingleAsync();

        Assert.Equal(1, saved.Revision);
        Assert.Equal(2, new HideoutStateSerializer().Deserialize<FarmState>(saved.Data).Level);
        Assert.Equal(1023, new HideoutStateSerializer().Deserialize<FarmState>(saved.Data).Slots[1].Seeds.Count);
        Assert.Equal(InventoryItemEnum.Raspberry, new HideoutStateSerializer().Deserialize<FarmState>(saved.Data).Slots[1].Ready);
    }

    [Fact]
    public async Task HideoutJson_PreservesUnrelatedPayloadWithoutFarmSchema()
    {
        await using var context = CreateContext();
        const string json = """{"fuel":{"kind":"wood","count":9},"temperature":42,"custom":[1,2]}""";
        context.CharacterHideouts.Add(new CharacterHideout { CharacterId = 43, Data = json });

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var saved = await context.CharacterHideouts.SingleAsync();

        Assert.Equal(json, saved.Data);
    }

    [Fact]
    public void FarmJson_RoundTripsIndependentPayload()
    {
        var state = new HideoutStateSerializer().Deserialize<FarmState>("""{"level":2,"revision":7}""");

        var json = new HideoutStateSerializer().Serialize(state);

        Assert.Equal(2, state.Level);
        Assert.Equal(7, state.Revision);
        Assert.DoesNotContain("\"farm\"", json);
        Assert.Equal(2, new HideoutStateSerializer().Deserialize<FarmState>(json).Level);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }
}
