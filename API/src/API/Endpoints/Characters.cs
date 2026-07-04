using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using ProjectX.API.Infrastructure;
using ProjectX.Application.Characters.Commands;
using ProjectX.Application.Characters.Queries.GetCharacter;
using ProjectX.Domain.Constants;

namespace ProjectX.API.Endpoints;

public class Characters : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder
            .MapGet(GetCharacter, "{id}")
            .RequireAuthorization(Policies.Server);

        groupBuilder
          .MapPost(UpdateCharacter)
          .RequireAuthorization(Policies.Server);
    }

    private static async Task<Ok<CharacterDto>> GetCharacter(ISender sender, int id)
    {
        var result = await sender.Send(new GetCharacterQuery(id));

        return TypedResults.Ok(result);
    }

    private static async Task<Ok> UpdateCharacter(ISender sender, UpdateCharacterCommand command)
    {
        await sender.Send(command);

        return TypedResults.Ok();
    }
}
