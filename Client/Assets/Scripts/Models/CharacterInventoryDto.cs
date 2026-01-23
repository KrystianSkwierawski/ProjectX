using System;
using System.Collections.Generic;

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
        public List<InventoryItem> items = new List<InventoryItem>(); 
    }
}