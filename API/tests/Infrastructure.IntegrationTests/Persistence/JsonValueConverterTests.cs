using Microsoft.EntityFrameworkCore;
using ProjectX.Domain.Crafting;
using ProjectX.Domain.Entities;
using ProjectX.Domain.Enums;
using ProjectX.Domain.Inventory;
using ProjectX.Infrastructure.Persistance;

namespace ProjectX.Infrastructure.IntegrationTests.Persistence;

public class JsonValueConverterTests
{
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

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }
}
