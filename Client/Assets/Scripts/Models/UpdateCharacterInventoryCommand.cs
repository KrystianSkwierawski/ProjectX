using System;

namespace Assets.Scripts.Models
{
    [Serializable]
    public class UpdateCharacterInventoryCommand
    {
        public int characterId;

        public InventoryDto inventory;
    }
}