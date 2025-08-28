using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using ProjectX.API.Infrastructure;
using ProjectX.Application.PlayerPositions.Commands.SavePlayerPosition;
using ProjectX.Application.PlayerPositions.Queries.GetPlayerPosition;
using ProjectX.Domain.Constants;

namespace ProjectX.API.Endpoints;

public class CharacterPositions : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetAsync).RequireAuthorization(Policies.Client);
        groupBuilder.MapPost(SaveAsync).RequireAuthorization(Policies.Server);
    }

    private static async Task<Ok<CharacterPositionDto>> GetAsync(ISender sender, [AsParameters] GetCharacterPositionQuery query)
    {
        var result = await sender.Send(query);

        return TypedResults.Ok(result);
    }

    private static async Task<NoContent> SaveAsync(ISender sender, SaveCharacterPositionCommand command)
    {
        await sender.Send(command);

        return TypedResults.NoContent();
    }   
}
