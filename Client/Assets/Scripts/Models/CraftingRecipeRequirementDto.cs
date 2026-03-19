using System;
using System.Collections.Generic;

namespace Assets.Scripts.Models
{
    [Serializable]
    public class CraftingRecipeRequirementDto
    {
        public List<InventoryItemDto> items;

        public int level;
    }
}
