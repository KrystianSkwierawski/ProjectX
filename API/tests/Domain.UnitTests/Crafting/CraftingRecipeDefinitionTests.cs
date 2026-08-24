using ProjectX.Domain.Enums;

namespace ProjectX.Domain.UnitTests.Crafting;

public class CraftingRecipeDefinitionTests
{
    [Fact]
    public void Definitions_AreCompleteAndUseMatchingRewardItems()
    {
        foreach (var recipe in Enum.GetValues<CraftingRecipeEnum>().Where(recipe => recipe != CraftingRecipeEnum.None))
        {
            var definition = recipe.GetDefinition();

            Assert.True(Enum.TryParse<InventoryItemEnum>(recipe.ToString(), out var rewardType));
            Assert.Equal(rewardType, definition.Reward.Item.Type);
            Assert.Equal(1, definition.Reward.Item.Count);
            Assert.Equal(StatusEnum.Active, definition.Status);
            Assert.True(definition.Requirement.Level > 0);
            Assert.All(definition.Requirement.Items, item => Assert.True(item.Count > 0));
        }
    }

    [Fact]
    public void AmmoArrow3_UsesExpectedTierThreeIngredients()
    {
        var definition = CraftingRecipeEnum.AmmoArrow3.GetDefinition();

        Assert.Equal(CraftingRecipeTypeEnum.Blacksmithing, definition.Type);
        Assert.Equal(3, definition.Requirement.Level);
        Assert.Equal(
            [(InventoryItemEnum.Wood, 3), (InventoryItemEnum.CopperBar, 3)],
            definition.Requirement.Items.Select(item => (item.Type, item.Count)));
    }

    [Theory]
    [InlineData(CraftingRecipeEnum.StrengthPotion, InventoryItemEnum.StrengthPotion)]
    [InlineData(CraftingRecipeEnum.SpeedPotion, InventoryItemEnum.SpeedPotion)]
    public void BuffPotions_AreAlchemyRecipesUsingChamomile(CraftingRecipeEnum recipe, InventoryItemEnum reward)
    {
        var definition = recipe.GetDefinition();

        Assert.Equal(CraftingRecipeTypeEnum.Alchemy, definition.Type);
        Assert.Equal(reward, definition.Reward.Item.Type);
        Assert.Equal(1, definition.Requirement.Level);
        Assert.Equal(
            [(InventoryItemEnum.Chamomile, 2)],
            definition.Requirement.Items.Select(item => (item.Type, item.Count)));
    }
}
