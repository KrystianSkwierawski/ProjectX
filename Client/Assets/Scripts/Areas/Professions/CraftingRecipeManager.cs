using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Assets.Scripts.Areas.Professions.Enums;
using Assets.Scripts.Areas.Professions.Models;
using Assets.Scripts.Areas.Shared.Mono;

namespace Assets.Scripts.Areas.Professions
{
    public class CraftingRecipeManager : Singleton<CraftingRecipeManager>
    {
        // TODO: cache manager with expiration?
        private IDictionary<CraftingRecipeTypeEnum, GetCraftingRecipesDto> _cache = new Dictionary<CraftingRecipeTypeEnum, GetCraftingRecipesDto>();

        public async UniTask<GetCraftingRecipesDto> GetAsync(CraftingRecipeTypeEnum type)
        {
            if (_cache.TryGetValue(type, out var result))
            {
                return result;
            }

            result = await UnityWebRequestHelper.ExecuteGetAsync<GetCraftingRecipesDto>($"CraftingRecipes?Type={type}");

            _cache.Add(type, result);

            return result;
        }
    }
}
