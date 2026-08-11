using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Caching.Memory;
using ProjectX.API.Infrastructure;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Application.Quests.Queries.GetQuest;
using ProjectX.Application.Quests.Queries.GetQuests;
using ProjectX.Domain.Enums;

namespace ProjectX.API.Endpoints;

public class Quests : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder
            .MapGet(GetQuest, "{id}")
            .WithSummary("Get a quest")
            .WithDescription("Returns one localized quest definition in the authenticated user's preferred language.")
            .WithParameterDescription("id", "Identifier of the quest to retrieve.")
            .WithResponseDescription(StatusCodes.Status200OK, "The localized quest definition was found and returned.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthorizationPolicies.Client);

        groupBuilder
            .MapGet(GetQuests)
            .WithSummary("Get active quests")
            .WithDescription("Returns all active quest definitions localized to the authenticated user's preferred language.")
            .WithResponseDescription(StatusCodes.Status200OK, "Active localized quest definitions were returned.")
            .RequireAuthorization(AuthorizationPolicies.ServerOrClient);
    }

    private static async Task<Ok<QuestDto>> GetQuest(
        IMemoryCache memoryCache,
        ICurrentUserService currentUserService,
        ISender sender,
        QuestEnum id,
        CancellationToken cancellationToken)
    {
        return await memoryCache.GetOrCreateAsync(ApiCacheKeys.Quest(id, currentUserService.Language), async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = ApiCacheKeys.Lifetime;
            var result = await sender.Send(new GetQuestQuery(id), cancellationToken);

            return TypedResults.Ok(result);
        }) ?? throw new InvalidOperationException("The quest cache factory returned null.");
    }

    private static async Task<Ok<GetQuestsDto>> GetQuests(
        IMemoryCache memoryCache,
        ICurrentUserService currentUserService,
        ISender sender,
        [AsParameters] GetQuestsQuery query,
        CancellationToken cancellationToken)
    {
        return await memoryCache.GetOrCreateAsync(ApiCacheKeys.Quests(currentUserService.Language), async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = ApiCacheKeys.Lifetime;
            var result = await sender.Send(query, cancellationToken);

            return TypedResults.Ok(result);
        }) ?? throw new InvalidOperationException("The quests cache factory returned null.");
    }
}
