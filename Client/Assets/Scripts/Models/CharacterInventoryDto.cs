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
        public IList<InventoryItem> Items { get; set; } = new List<InventoryItem>();
    }
}