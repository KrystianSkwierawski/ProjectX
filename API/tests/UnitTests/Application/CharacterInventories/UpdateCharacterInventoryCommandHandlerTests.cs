using ProjectX.Application.CharacterInventories.Commands.UpdateCharacterInventory;
using ProjectX.Application.CharacterInventories.Queries.GetCharacterInventory;
using ProjectX.Domain.Enums;

namespace ProjectX.UnitTests.Application.CharacterInventories;

public class UpdateCharacterInventoryCommandHandlerTests
{
    [Fact]
    public void Split_AppendsHalfOfEvenStackToNextFreeSlot()
    {
        var inventory = CreateInventory(
            new InventoryItemDto { Type = InventoryItemEnum.HealthPotion, Count = 8 },
            new InventoryItemDto { Type = InventoryItemEnum.Currency, Count = 3 });

        var result = UpdateCharacterInventoryCommandHandler.Split(0, inventory, 4);

        Assert.True(result);
        Assert.Equal(3, inventory.Items.Count);
        Assert.Equal(4, inventory.Items[0].Count);
        Assert.Equal(InventoryItemEnum.HealthPotion, inventory.Items[2].Type);
        Assert.Equal(4, inventory.Items[2].Count);
    }

    [Fact]
    public void Split_LeavesLargerHalfInSourceSlotForOddStack()
    {
        var inventory = CreateInventory(
            new InventoryItemDto { Type = InventoryItemEnum.HealthPotion, Count = 5 });

        var result = UpdateCharacterInventoryCommandHandler.Split(0, inventory, 2);

        Assert.True(result);
        Assert.Equal(3, inventory.Items[0].Count);
        Assert.Equal(2, inventory.Items[1].Count);
    }

    [Fact]
    public void Split_DoesNotChangeInventoryWhenThereIsNoFreeSlot()
    {
        var inventory = CreateInventory(
            new InventoryItemDto { Type = InventoryItemEnum.HealthPotion, Count = 8 },
            new InventoryItemDto { Type = InventoryItemEnum.Currency, Count = 3 });

        var result = UpdateCharacterInventoryCommandHandler.Split(0, inventory, 2);

        Assert.False(result);
        Assert.Equal(2, inventory.Items.Count);
        Assert.Equal(8, inventory.Items[0].Count);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void Split_DoesNotChangeInventoryForInvalidSourceSlot(int sourceSlotIndex)
    {
        var inventory = CreateInventory(
            new InventoryItemDto { Type = InventoryItemEnum.HealthPotion, Count = 8 });

        var result = UpdateCharacterInventoryCommandHandler.Split(sourceSlotIndex, inventory, 2);

        Assert.False(result);
        Assert.Single(inventory.Items);
        Assert.Equal(8, inventory.Items[0].Count);
    }

    [Fact]
    public void Split_DoesNotChangeSingleItemStack()
    {
        var inventory = CreateInventory(
            new InventoryItemDto { Type = InventoryItemEnum.HealthPotion, Count = 1 });

        var result = UpdateCharacterInventoryCommandHandler.Split(0, inventory, 2);

        Assert.False(result);
        Assert.Single(inventory.Items);
        Assert.Equal(1, inventory.Items[0].Count);
    }

    private static InventoryDto CreateInventory(params InventoryItemDto[] items)
    {
        return new InventoryDto
        {
            Items = items.ToList(),
        };
    }
}
