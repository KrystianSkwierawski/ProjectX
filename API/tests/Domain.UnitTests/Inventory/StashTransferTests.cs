using ProjectX.Domain.Enums;
using ProjectX.Domain.Inventory;

namespace ProjectX.Domain.UnitTests.Inventory;

public class StashTransferTests
{
    [Fact]
    public void AutomaticTransfer_FillsPartialStacksAndKeepsSourcePosition()
    {
        var source = new InventoryState([new(InventoryItemEnum.HealthPotion, 40), new(InventoryItemEnum.Fish, 3)]);
        var target = new InventoryState([new(InventoryItemEnum.HealthPotion, 1000)]);

        Assert.True(source.TransferTo(target, 0, 2));

        Assert.True(source.Items[0].IsEmpty);
        Assert.Equal(InventoryItemEnum.Fish, source.Items[1].Type);
        Assert.Equal(new[] { 1024, 16 }, target.Items.Select(x => x.Count));
    }

    [Theory]
    [InlineData(InventoryItemEnum.HealthPotion, 1000)]
    [InlineData(InventoryItemEnum.Fish, 1)]
    public void TargetedTransfer_RejectsWithoutPartialRemoval(InventoryItemEnum type, int count)
    {
        var source = new InventoryState([new(InventoryItemEnum.HealthPotion, 40)]);
        var target = new InventoryState([new(type, count)]);

        Assert.False(source.TransferTo(target, 0, 64, 0));

        Assert.Equal(40, source.Items[0].Count);
        Assert.Equal(count, target.Items[0].Count);
    }
}
