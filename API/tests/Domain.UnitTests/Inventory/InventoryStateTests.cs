using ProjectX.Domain.Enums;
using ProjectX.Domain.Inventory;

namespace ProjectX.Domain.UnitTests.Inventory;

public class InventoryStateTests
{
    [Fact]
    public void Split_AppendsHalfOfEvenStackToNextFreeSlot()
    {
        var inventory = CreateInventory(
            new InventorySlot(InventoryItemEnum.HealthPotion, 8),
            new InventorySlot(InventoryItemEnum.Currency, 3));

        var result = inventory.Split(0, 4);

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
            new InventorySlot(InventoryItemEnum.HealthPotion, 5));

        var result = inventory.Split(0, 2);

        Assert.True(result);
        Assert.Equal(3, inventory.Items[0].Count);
        Assert.Equal(2, inventory.Items[1].Count);
    }

    [Fact]
    public void Split_DoesNotChangeInventoryWhenThereIsNoFreeSlot()
    {
        var inventory = CreateInventory(
            new InventorySlot(InventoryItemEnum.HealthPotion, 8),
            new InventorySlot(InventoryItemEnum.Currency, 3));

        var result = inventory.Split(0, 2);

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
            new InventorySlot(InventoryItemEnum.HealthPotion, 8));

        var result = inventory.Split(sourceSlotIndex, 2);

        Assert.False(result);
        Assert.Single(inventory.Items);
        Assert.Equal(8, inventory.Items[0].Count);
    }

    [Fact]
    public void Split_DoesNotChangeSingleItemStack()
    {
        var inventory = CreateInventory(
            new InventorySlot(InventoryItemEnum.HealthPotion, 1));

        var result = inventory.Split(0, 2);

        Assert.False(result);
        Assert.Single(inventory.Items);
        Assert.Equal(1, inventory.Items[0].Count);
    }

    [Fact]
    public void Move_SwapsDifferentItems()
    {
        var inventory = CreateInventory(
            new InventorySlot(InventoryItemEnum.HealthPotion, 4),
            new InventorySlot(InventoryItemEnum.Currency, 10));

        var result = inventory.Move(0, 1, 4);

        Assert.True(result);
        Assert.Equal(InventoryItemEnum.Currency, inventory.Items[0].Type);
        Assert.Equal(10, inventory.Items[0].Count);
        Assert.Equal(InventoryItemEnum.HealthPotion, inventory.Items[1].Type);
        Assert.Equal(4, inventory.Items[1].Count);
    }

    [Fact]
    public void Move_MergesStacksOfTheSameItem()
    {
        var inventory = CreateInventory(
            new InventorySlot(InventoryItemEnum.HealthPotion, 4),
            new InventorySlot(InventoryItemEnum.HealthPotion, 3));

        var result = inventory.Move(0, 1, 4);

        Assert.True(result);
        Assert.Equal(InventoryItemEnum.None, inventory.Items[0].Type);
        Assert.Equal(0, inventory.Items[0].Count);
        Assert.Equal(InventoryItemEnum.HealthPotion, inventory.Items[1].Type);
        Assert.Equal(7, inventory.Items[1].Count);
    }

    [Fact]
    public void Move_PreservesTheSelectedEmptySlot()
    {
        var inventory = CreateInventory(
            new InventorySlot(InventoryItemEnum.HealthPotion, 4));

        var result = inventory.Move(0, 3, 4);

        Assert.True(result);
        Assert.Equal(4, inventory.Items.Count);
        Assert.All(inventory.Items.Take(3), item => Assert.Equal(InventoryItemEnum.None, item.Type));
        Assert.Equal(InventoryItemEnum.HealthPotion, inventory.Items[3].Type);
        Assert.Equal(4, inventory.Items[3].Count);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(1, 0)]
    [InlineData(0, -1)]
    [InlineData(0, 4)]
    [InlineData(0, 0)]
    public void Move_DoesNotChangeInventoryForInvalidSlots(int sourceSlotIndex, int targetSlotIndex)
    {
        var inventory = CreateInventory(
            new InventorySlot(InventoryItemEnum.HealthPotion, 4));

        var result = inventory.Move(sourceSlotIndex, targetSlotIndex, 4);

        Assert.False(result);
        Assert.Single(inventory.Items);
        Assert.Equal(InventoryItemEnum.HealthPotion, inventory.Items[0].Type);
        Assert.Equal(4, inventory.Items[0].Count);
    }

    [Fact]
    public void Remove_ClearsSlotWithoutShiftingFollowingItems()
    {
        var inventory = CreateInventory(
            new InventorySlot(InventoryItemEnum.HealthPotion, 4),
            new InventorySlot(InventoryItemEnum.Currency, 10));

        var result = inventory.Remove(InventoryItemEnum.HealthPotion, 4);

        Assert.True(result);
        Assert.Equal(2, inventory.Items.Count);
        Assert.Equal(InventoryItemEnum.None, inventory.Items[0].Type);
        Assert.Equal(InventoryItemEnum.Currency, inventory.Items[1].Type);
    }

    [Fact]
    public void Add_FillsFirstEmptySlot()
    {
        var inventory = CreateInventory(
            InventorySlot.Empty(),
            new InventorySlot(InventoryItemEnum.Currency, 10));

        var result = inventory.Add(InventoryItemEnum.HealthPotion, 4);

        Assert.True(result);
        Assert.Equal(2, inventory.Items.Count);
        Assert.Equal(InventoryItemEnum.HealthPotion, inventory.Items[0].Type);
        Assert.Equal(4, inventory.Items[0].Count);
        Assert.Equal(InventoryItemEnum.Currency, inventory.Items[1].Type);
    }

    [Fact]
    public void Add_AppendsItemWhenThereIsNoEmptySlot()
    {
        var inventory = CreateInventory(
            new InventorySlot(InventoryItemEnum.Currency, 10));

        var result = inventory.Add(InventoryItemEnum.HealthPotion, 4);

        Assert.True(result);
        Assert.Equal(2, inventory.Items.Count);
        Assert.Equal(InventoryItemEnum.HealthPotion, inventory.Items[1].Type);
        Assert.Equal(4, inventory.Items[1].Count);
    }

    [Fact]
    public void Remove_ConsumesMultipleStacks()
    {
        var inventory = CreateInventory(
            new InventorySlot(InventoryItemEnum.HealthPotion, 2),
            new InventorySlot(InventoryItemEnum.HealthPotion, 3));

        var result = inventory.Remove(InventoryItemEnum.HealthPotion, 4);

        Assert.True(result);
        Assert.Equal(InventoryItemEnum.None, inventory.Items[0].Type);
        Assert.Equal(1, inventory.Items[1].Count);
    }

    [Fact]
    public void Remove_DoesNotMutateInventoryWhenTotalCountIsInsufficient()
    {
        var inventory = CreateInventory(
            new InventorySlot(InventoryItemEnum.HealthPotion, 2),
            new InventorySlot(InventoryItemEnum.HealthPotion, 1));

        var result = inventory.Remove(InventoryItemEnum.HealthPotion, 4);

        Assert.False(result);
        Assert.Equal(2, inventory.Items[0].Count);
        Assert.Equal(1, inventory.Items[1].Count);
    }

    private static InventoryState CreateInventory(params InventorySlot[] items)
    {
        return new InventoryState(items);
    }
}
