using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using ProjectX.API.Infrastructure;
using ProjectX.Application.Quests.Queries.GetQuest;
using ProjectX.Domain.Constants;

namespace ProjectX.API.Endpoints;

public class Quest : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetQuestQuery).RequireAuthorization(Policies.Client);
    }

    private static async Task<Ok<QuestoDto>> GetQuestQuery(ISender sender, [AsParameters] GetQuestQuery query)
    {
        var result = await sender.Send(query);

        return TypedResults.Ok(result);
    } 
}
