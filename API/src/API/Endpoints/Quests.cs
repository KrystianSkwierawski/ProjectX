using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Caching.Memory;
using ProjectX.API.Infrastructure;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Application.Extensions;
using ProjectX.Application.Quests.Queries.GetQuest;
using ProjectX.Application.Quests.Queries.GetQuests;
using ProjectX.Domain.Constants;
using ProjectX.Domain.Enums;

namespace ProjectX.API.Endpoints;

public class Quests : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder
            .MapGet(GetQuest, "{id}")
            .RequireAuthorization(Policies.Client);

        groupBuilder
            .MapGet(GetQuests)
            .RequireAuthorization(Policies.Client);
    }

    private static async Task<Ok<QuestDto>> GetQuest(IMemoryCache memoryCache, ICurrentUserService currentUserService, ISender sender, QuestEnum id)
    {
        return await memoryCache.GetOrCreateAsync(CacheKeyEnum.Quest, async (ICacheEntry entry) =>
        {
            var result = await sender.Send(new GetQuestQuery(id));

            return TypedResults.Ok(result);
        }, id, currentUserService.Language);
    }

    private static async Task<Ok<GetQuestsDto>> GetQuests(IMemoryCache memoryCache, ICurrentUserService currentUserService, ISender sender, [AsParameters] GetQuestsQuery query)
    {
        return await memoryCache.GetOrCreateAsync(CacheKeyEnum.Quests, async (ICacheEntry entry) =>
        {
            var result = await sender.Send(query);

            return TypedResults.Ok(result);
        }, currentUserService.Language);
    }
}
