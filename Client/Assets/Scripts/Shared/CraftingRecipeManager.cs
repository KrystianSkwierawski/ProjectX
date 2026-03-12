using System.Collections.Generic;
using Assets.Scripts.Enums;
using Assets.Scripts.Models;
using Cysharp.Threading.Tasks;

namespace Assets.Scripts.Shared
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

            result = await UnityWebRequestHelper.ExecuteGetAsync<GetCraftingRecipesDto>($"CraftingRecipes?Type={type}", UserManager.Instance.Token);

            _cache.Add(type, result);

            return result;
        }
    }
}
