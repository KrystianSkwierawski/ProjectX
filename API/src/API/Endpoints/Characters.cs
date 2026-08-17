using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using ProjectX.API.Infrastructure;
using ProjectX.Application.Characters.Commands;
using ProjectX.Application.Characters.Queries.GetCharacter;
using ProjectX.Application.Characters.Queries.GetCharacters;

namespace ProjectX.API.Endpoints;

public class Characters : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder
            .MapGet(GetCharacters)
            .WithSummary("Get characters")
            .WithDescription("Returns the authenticated user's playable characters.")
            .RequireAuthorization(AuthorizationPolicies.Client);

        groupBuilder
            .MapGet(GetCharacter, "Current")
            .WithSummary("Get character")
            .WithDescription("Returns the selected character's current state.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthorizationPolicies.ServerPlayerSession);

        groupBuilder
            .MapPost(UpdateCharacter)
            .WithSummary("Update character")
            .WithDescription("Updates a character's persistent state.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthorizationPolicies.ServerPlayerSession);
    }

    public static async Task<Ok<GetCharactersDto>> GetCharacters(ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCharactersQuery(), cancellationToken);

        return TypedResults.Ok(result);
    }

    public static async Task<Ok<CharacterDto>> GetCharacter(ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCharacterQuery(), cancellationToken);

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
