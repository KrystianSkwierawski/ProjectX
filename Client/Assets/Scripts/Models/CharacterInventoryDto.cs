using System;

namespace Assets.Scripts.Models
{
    [Serializable]
    public class CharacterInventoryDto
    {
        public int characterId;

        public InventoryDto inventory;

        public short count;
    }

    [Serializable]
    public class InventoryDto
    {
        public InventoryItem[] items = new InventoryItem[] {}; 
    }
}