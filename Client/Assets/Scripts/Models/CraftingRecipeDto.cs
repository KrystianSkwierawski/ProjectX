using Assets.Scripts.Enums;

namespace Assets.Scripts.Models
{
    public class CraftingRecipeDto
    {
        public CraftingRecipeEnum Id { get; set; }

        public CraftingRecipeRequirementDto Requirement { get; set; }

        public CraftingRecipeRewardDto Reward { get; set; }
    }
}
