using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using ProjectX.API.Infrastructure;
using ProjectX.Application.Extensions;
using ProjectX.Application.CraftingRecipes.Queries.GetCraftingRecipes;
using ProjectX.Domain.Constants;
using ProjectX.Domain.Enums;
using Microsoft.Extensions.Caching.Memory;

namespace ProjectX.API.Endpoints;

public class CraftingRecipes : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder
            .MapGet(GetCraftingRecipes)
            .RequireAuthorization(Policies.ServerOrClient);
    }

    private static async Task<Ok<GetCraftingRecipesDto>> GetCraftingRecipes(IMemoryCache memoryCache, ISender sender, [AsParameters] GetCraftingRecipesQuery query)
    {
        return await memoryCache.GetOrCreateAsync(CacheKeyEnum.CraftingRecipes, async entry =>
        {
            var result = await sender.Send(query);

            return TypedResults.Ok(result);
        }, query.type);
    }
}
