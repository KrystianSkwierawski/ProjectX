using Assets.Scripts.Areas.Professions.Enums;

namespace Assets.Scripts.Areas.Professions.Models
{
    public class CraftingRecipeDto
    {
        public CraftingRecipeEnum Id { get; set; }

        public CraftingRecipeRequirementDto Requirement { get; set; }

        public CraftingRecipeRewardDto Reward { get; set; }
    }
}
