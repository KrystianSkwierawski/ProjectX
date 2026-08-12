using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Caching.Memory;
using ProjectX.API.Infrastructure;
using ProjectX.Application.CraftingRecipes.Queries.GetCraftingRecipes;
using ProjectX.Domain.Enums;

namespace ProjectX.API.Endpoints;

public class CraftingRecipes : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder
            .MapGet(GetCraftingRecipes)
            .WithSummary("Get crafting recipes")
            .WithDescription("Returns active recipes for a crafting profession.")
            .RequireAuthorization(AuthorizationPolicies.ServerOrClient);
    }

    public static async Task<Ok<GetCraftingRecipesDto>> GetCraftingRecipes(
        IMemoryCache memoryCache,
        ISender sender,
        [AsParameters] GetCraftingRecipesQuery query,
        CancellationToken cancellationToken)
    {
        return await memoryCache.GetOrCreateAsync(ApiCacheKeys.CraftingRecipes(query.Type), async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = ApiCacheKeys.Lifetime;
            var result = await sender.Send(query, cancellationToken);

            return TypedResults.Ok(result);
        }) ?? throw new InvalidOperationException("The crafting-recipe cache factory returned null.");
    }
}
