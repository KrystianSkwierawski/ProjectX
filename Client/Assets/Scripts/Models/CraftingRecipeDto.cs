using System;

namespace Assets.Scripts.Models
{
    [Serializable]
    public class CraftingRecipeDto
    {
        public CraftingRecipeRequirementDto requirement;

        public CraftingRecipeRewardDto reward;
    }
}
