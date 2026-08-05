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
            .WithSummary("Get crafting recipes")
            .WithDescription("Returns active crafting recipes for the requested profession, ordered by recipe identifier.")
            .WithParameterDescription("type", "Crafting profession used to filter active recipes.")
            .WithResponseDescription(StatusCodes.Status200OK, "Matching active crafting recipes were returned.")
            .RequireAuthorization(Policies.ServerOrClient);
    }

    private static async Task<Ok<GetCraftingRecipesDto>> GetCraftingRecipes(
        IMemoryCache memoryCache,
        ISender sender,
        [AsParameters] GetCraftingRecipesQuery query,
        CancellationToken cancellationToken)
    {
        return await memoryCache.GetOrCreateAsync(CacheKeyEnum.CraftingRecipes, async entry =>
        {
            var result = await sender.Send(query, cancellationToken);

            return TypedResults.Ok(result);
        }, query.type);
    }
}
