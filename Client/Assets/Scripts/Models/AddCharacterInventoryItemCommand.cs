using System;

namespace Assets.Scripts.Models
{
    [Serializable]
    public class AddCharacterInventoryItemCommand
    {
        public int characterId;

        public InventoryItem inventoryItem;
    }
}
