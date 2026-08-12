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
            .WithSummary("Get character")
            .WithDescription("Returns a character's current state.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthorizationPolicies.ServerPlayerSession);

        groupBuilder
            .MapPost(UpdateCharacter)
            .WithSummary("Update character")
            .WithDescription("Updates a character's persistent state.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthorizationPolicies.ServerPlayerSession);
    }

    public static async Task<Ok<CharacterDto>> GetCharacter(ISender sender, int id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCharacterQuery(id), cancellationToken);

        return TypedResults.Ok(result);
    }

    public static async Task<NoContent> UpdateCharacter(
        ISender sender,
        UpdateCharacterCommand command,
        CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);

        return TypedResults.NoContent();
    }
}
