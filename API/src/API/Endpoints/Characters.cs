using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using ProjectX.API.Infrastructure;
using ProjectX.Application.Characters.Queries;
using ProjectX.Domain.Constants;

namespace ProjectX.API.Endpoints;

public class Characters : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetCharacterQuery, "{id}").RequireAuthorization(Policies.Client);
    }

    private static async Task<Ok<CharacterDto>> GetCharacterQuery(ISender sender, int id)
    {
        var result = await sender.Send(new GetCharacterQuery(id));

        return TypedResults.Ok(result);
    }
}
