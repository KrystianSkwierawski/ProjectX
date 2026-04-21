using System.Collections.Generic;

namespace Assets.Scripts.Models
{
    public class CharacterInventoryDto
    {
        public int CharacterId { get; set; }

        public InventoryDto Inventory { get; set; }

        public short Count { get; set; }
    }

    public class InventoryDto
    {
        public IList<InventoryItemDto> Items { get; set; }
    }
}