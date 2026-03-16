using System;
using Assets.Scripts.Enums;

namespace Assets.Scripts.Models
{
    [Serializable]
    public class CraftingRecipeDto
    {
        public CraftingRecipeEnum id;

        public CraftingRecipeRequirementDto requirement;

        public CraftingRecipeRewardDto reward;
    }
}
