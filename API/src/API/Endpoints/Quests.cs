using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using ProjectX.API.Infrastructure;
using ProjectX.Application.Quests.Queries.GetQuest;
using ProjectX.Domain.Constants;

namespace ProjectX.API.Endpoints;

public class Quests : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetQuestQuery, "{id}").RequireAuthorization(Policies.Client);
    }

    private static async Task<Ok<QuestoDto>> GetQuestQuery(ISender sender, int id)
    {
        var result = await sender.Send(new GetQuestQuery(id));

        return TypedResults.Ok(result);
    } 
}
