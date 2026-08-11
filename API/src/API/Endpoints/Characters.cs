using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using ProjectX.API.Infrastructure;
using ProjectX.Application.Characters.Commands;
using ProjectX.Application.Characters.Queries.GetCharacter;

namespace ProjectX.API.Endpoints;

public class Characters : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder
            .MapGet(GetCharacter, "{id}")
            .WithSummary("Get a character")
            .WithDescription("Returns current persistent state, attributes, equipment, and progression levels for a character owned by the authenticated user.")
            .WithParameterDescription("id", "Identifier of the character to retrieve.")
            .WithResponseDescription(StatusCodes.Status200OK, "The character was found and returned.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthorizationPolicies.ServerPlayerSession);

        groupBuilder
            .MapPost(UpdateCharacter)
            .WithSummary("Update a character")
            .WithDescription("Partially updates persistent health, attributes, equipment, or ammunition state for a character.")
            .WithRequestBodyDescription("Character identifier and the optional state fields to update.")
            .WithResponseDescription(StatusCodes.Status204NoContent, "The character update was persisted.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthorizationPolicies.ServerPlayerSession);
    }

    private static async Task<Ok<CharacterDto>> GetCharacter(ISender sender, int id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCharacterQuery(id), cancellationToken);

        return TypedResults.Ok(result);
    }

    private static async Task<NoContent> UpdateCharacter(
        ISender sender,
        UpdateCharacterCommand command,
        CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);

        return TypedResults.NoContent();
    }
}
