using ProjectX.Application.CharacterInventories.Queries.GetCharacterInventory;

namespace ProjectX.Application.CharacterInventories.Commands.TradeCharacterInventories;

public class TradeCharacterInventoriesDto
{
    public TradeCharacterInventoriesStatusEnum Status { get; set; }

    public CharacterInventoryDto? SourceInventory { get; set; }

    public CharacterInventoryDto? TargetInventory { get; set; }
}

public enum TradeCharacterInventoriesStatusEnum
{
    Applied,
    SourceInventoryFull,
    TargetInventoryFull,
    SourceItemsUnavailable,
    TargetItemsUnavailable,
    InventoryChanged,
    ReceiptNotFound
}
