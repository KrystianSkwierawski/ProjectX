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
            .WithSummary("Get a character inventory")
            .WithDescription("Returns the persisted ordered inventory slots and capacity for the authenticated user's character.")
            .WithParameterDescription("CharacterId", "Identifier of the character whose inventory is requested.")
            .WithResponseDescription(StatusCodes.Status200OK, "The character inventory was found and returned.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthorizationPolicies.Client);

        groupBuilder
            .MapPost(UpdateCharacterInventory)
            .WithSummary("Update a character inventory")
            .WithDescription("Applies item additions, removals, a stack split, or a slot move to the persisted character inventory.")
            .WithRequestBodyDescription("Character identifier and inventory operations to apply.")
            .WithResponseDescription(StatusCodes.Status204NoContent, "The inventory update was persisted.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthorizationPolicies.ServerPlayerSession);
    }

    private static async Task<Ok<CharacterInventoryDto>> GetCharacterInventory(
        ISender sender,
        [AsParameters] GetCharacterInventoryQuery query,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(query, cancellationToken);

        return TypedResults.Ok(result);
    }

    private static async Task<NoContent> UpdateCharacterInventory(
        ISender sender,
        UpdateCharacterInventoryCommand command,
        CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);

        return TypedResults.NoContent();
    }
}
