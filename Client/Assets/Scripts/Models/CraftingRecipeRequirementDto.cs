using System;
using System.Collections.Generic;

namespace Assets.Scripts.Models
{
    [Serializable]
    public class CraftingRecipeRequirementDto
    {
        public List<InventoryItem> items { get; set; }

        public int level { get; set; }
    }
}
