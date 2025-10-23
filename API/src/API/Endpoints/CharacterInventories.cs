using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using ProjectX.API.Infrastructure;
using ProjectX.Application.CharacterInventories.Commands;
using ProjectX.Application.CharacterInventories.Queries.GetCharacterInventory;
using ProjectX.Domain.Constants;

namespace ProjectX.API.Endpoints;

public class CharacterInventories : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetCharacterInventory).RequireAuthorization(Policies.Client);
        groupBuilder.MapPut(UpdateCharacterInventory, "/").RequireAuthorization(Policies.Server);
    }

    private static async Task<Ok<CharacterInventoryDto>> GetCharacterInventory(ISender sender, [AsParameters] GetCharacterInventoryQuery query)
    {
        var result = await sender.Send(query);

        return TypedResults.Ok(result);
    }

    private static async Task<NoContent> UpdateCharacterInventory(ISender sender, UpdateCharacterInventoryCommand command)
    {
        await sender.Send(command);

        return TypedResults.NoContent();
    }
}
