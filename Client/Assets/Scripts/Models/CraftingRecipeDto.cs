using System;

namespace Assets.Scripts.Models
{
    [Serializable]
    public class CraftingRecipeDto
    {
        public InventoryItemEnum inventoryItemId;


        public CraftingRecipeRequirementDto requirement;

        public CraftingRecipeRewardDto reward;
    }
}
