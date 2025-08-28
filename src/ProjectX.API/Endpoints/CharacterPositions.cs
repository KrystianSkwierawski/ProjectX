using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using ProjectX.API.Infrastructure;
using ProjectX.Application.PlayerPositions.Commands.SavePlayerPosition;
using ProjectX.Application.PlayerPositions.Queries.GetPlayerPosition;

namespace ProjectX.API.Endpoints;

public class CharacterPositions : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        //groupBuilder.RequireAuthorization();

        groupBuilder.MapGet(GetAsync, "{id}");
        groupBuilder.MapPost(SaveAsync);
    }

    private static async Task<Ok<CharacterPositionDto>> GetAsync(ISender sender, int id)
    {
        var result = await sender.Send(new GetCharacterPositionQuery(id));

        return TypedResults.Ok(result);
    }

    private static async Task<NoContent> SaveAsync(ISender sender, SaveCharacterPositionCommand command)
    {
        await sender.Send(command);

        return TypedResults.NoContent();
    }   
}
