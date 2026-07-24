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

    [Fact]
    public void Move_SwapsDifferentItems()
    {
        var inventory = CreateInventory(
            new InventoryItemDto { Type = InventoryItemEnum.HealthPotion, Count = 4 },
            new InventoryItemDto { Type = InventoryItemEnum.Currency, Count = 10 });

        var result = UpdateCharacterInventoryCommandHandler.Move(0, 1, inventory, 4);

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
            new InventoryItemDto { Type = InventoryItemEnum.HealthPotion, Count = 4 },
            new InventoryItemDto { Type = InventoryItemEnum.HealthPotion, Count = 3 });

        var result = UpdateCharacterInventoryCommandHandler.Move(0, 1, inventory, 4);

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
            new InventoryItemDto { Type = InventoryItemEnum.HealthPotion, Count = 4 });

        var result = UpdateCharacterInventoryCommandHandler.Move(0, 3, inventory, 4);

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
            new InventoryItemDto { Type = InventoryItemEnum.HealthPotion, Count = 4 });

        var result = UpdateCharacterInventoryCommandHandler.Move(
            sourceSlotIndex,
            targetSlotIndex,
            inventory,
            4);

        Assert.False(result);
        Assert.Single(inventory.Items);
        Assert.Equal(InventoryItemEnum.HealthPotion, inventory.Items[0].Type);
        Assert.Equal(4, inventory.Items[0].Count);
    }

    [Fact]
    public void Remove_ClearsSlotWithoutShiftingFollowingItems()
    {
        var inventory = CreateInventory(
            new InventoryItemDto { Type = InventoryItemEnum.HealthPotion, Count = 4 },
            new InventoryItemDto { Type = InventoryItemEnum.Currency, Count = 10 });

        var result = UpdateCharacterInventoryCommandHandler.Remove(
            new InventoryItemDto { Type = InventoryItemEnum.HealthPotion, Count = 4 },
            inventory);

        Assert.True(result);
        Assert.Equal(2, inventory.Items.Count);
        Assert.Equal(InventoryItemEnum.None, inventory.Items[0].Type);
        Assert.Equal(InventoryItemEnum.Currency, inventory.Items[1].Type);
    }

    [Fact]
    public void Add_FillsFirstEmptySlot()
    {
        var inventory = CreateInventory(
            new InventoryItemDto { Type = InventoryItemEnum.None, Count = 0 },
            new InventoryItemDto { Type = InventoryItemEnum.Currency, Count = 10 });

        var result = UpdateCharacterInventoryCommandHandler.Add(
            new InventoryItemDto { Type = InventoryItemEnum.HealthPotion, Count = 4 },
            inventory);

        Assert.True(result);
        Assert.Equal(2, inventory.Items.Count);
        Assert.Equal(InventoryItemEnum.HealthPotion, inventory.Items[0].Type);
        Assert.Equal(4, inventory.Items[0].Count);
        Assert.Equal(InventoryItemEnum.Currency, inventory.Items[1].Type);
    }

    private static InventoryDto CreateInventory(params InventoryItemDto[] items)
    {
        return new InventoryDto
        {
            Items = items.ToList(),
        };
    }
}
