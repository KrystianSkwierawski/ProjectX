using System;
using System.Collections.Generic;

namespace Assets.Scripts.Models
{
    [Serializable]
    public class GetCraftingRecipesDto
    {
        public List<CraftingRecipeDto> craftingRecipes;
    }
}
