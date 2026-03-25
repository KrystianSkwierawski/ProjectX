using System;

namespace Assets.Scripts.Models
{
    [Serializable]
    public class RemoveCharacterInventoryItemCommand
    {
        public int characterId;

        public InventoryItemDto inventoryItem;
    }
}
