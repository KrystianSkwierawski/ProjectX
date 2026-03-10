using System.Collections.Generic;
using Assets.Scripts.Enums;
using Assets.Scripts.Models;
using Cysharp.Threading.Tasks;

namespace Assets.Scripts.Shared
{
    public class CraftingRecipeManager : Singleton<CraftingRecipeManager>
    {
        // TODO: cache manager with expiration?
        private IDictionary<CraftingRecipieTypeEnum, GetCraftingRecipesDto> _cache = new Dictionary<CraftingRecipieTypeEnum, GetCraftingRecipesDto>();

        public async UniTask<GetCraftingRecipesDto> GetAsync(CraftingRecipieTypeEnum type)
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
