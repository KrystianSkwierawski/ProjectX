using System;
using Assets.Scripts.Areas.Inventory.Models;

namespace Assets.Scripts.Areas.Trade.Models
{
    public class TradeCharacterInventoriesCommand
    {
        public Guid TradeId { get; set; }

        public string OtherPlayerSessionId { get; set; }

        public InventoryItemDto[] Give { get; set; } = Array.Empty<InventoryItemDto>();

        public InventoryItemDto[] Receive { get; set; } = Array.Empty<InventoryItemDto>();
    }

    public class TradeCharacterInventoriesDto
    {
        public TradeCharacterInventoriesStatusEnum Status { get; set; }

        public CharacterInventoryDto SourceInventory { get; set; }

        public CharacterInventoryDto TargetInventory { get; set; }
    }

    public class ResolveCharacterInventoryTradeCommand
    {
        public Guid TradeId { get; set; }
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
}
