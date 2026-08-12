using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using ProjectX.API.Infrastructure;
using ProjectX.Application.CharacterInventories.Commands.UpdateCharacterInventory;
using ProjectX.Application.CharacterInventories.Queries.GetCharacterInventory;

namespace ProjectX.API.Endpoints;

public class CharacterInventories : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder
            .MapGet(GetCharacterInventory)
            .WithSummary("Get character inventory")
            .WithDescription("Returns a character's inventory and capacity.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthorizationPolicies.Client);

        groupBuilder
            .MapPost(UpdateCharacterInventory)
            .WithSummary("Update character inventory")
            .WithDescription("Applies inventory changes for a character.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthorizationPolicies.ServerPlayerSession);
    }

    public static async Task<Ok<CharacterInventoryDto>> GetCharacterInventory(
        ISender sender,
        [AsParameters] GetCharacterInventoryQuery query,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(query, cancellationToken);

        return TypedResults.Ok(result);
    }

    public static async Task<NoContent> UpdateCharacterInventory(
        ISender sender,
        UpdateCharacterInventoryCommand command,
        CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);

        return TypedResults.NoContent();
    }
}
