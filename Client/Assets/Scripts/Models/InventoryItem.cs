using System;
using Assets.Scripts.Enums;

namespace Assets.Scripts.Models
{
    [Serializable]
    public class InventoryItem
    {
        public CharacterInventoryTypeEnum type;

        public int count;
    }
}

