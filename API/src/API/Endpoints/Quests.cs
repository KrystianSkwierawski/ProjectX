using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using ProjectX.API.Infrastructure;
using ProjectX.Application.Quests.Queries.GetQuest;
using ProjectX.Application.Quests.Queries.GetQuests;
using ProjectX.Domain.Constants;

namespace ProjectX.API.Endpoints;

public class Quests : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetQuest, "{id}").RequireAuthorization(Policies.Client);
        groupBuilder.MapGet(GetQuests).RequireAuthorization(Policies.Client);
    }

    private static async Task<Ok<QuestoDto>> GetQuest(ISender sender, int id)
    {
        var result = await sender.Send(new GetQuestQuery(id));

        return TypedResults.Ok(result);
    }

    private static async Task<Ok<GetQuestsDto>> GetQuests(ISender sender, [AsParameters] GetQuestsQuery query)
    {
        var result = await sender.Send(query);

        return TypedResults.Ok(result);
    }
}
