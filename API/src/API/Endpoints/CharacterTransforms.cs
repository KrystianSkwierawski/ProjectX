using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using ProjectX.API.Infrastructure;
using ProjectX.Application.CharacterTransforms.Commands.SaveCharacterTransform;
using ProjectX.Application.CharacterTransforms.Queries.GetCharacterTransform;
using ProjectX.Domain.Constants;

namespace ProjectX.API.Endpoints;

public class CharacterTransforms : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder
            .MapGet(GetCharacterTransform)
            .WithSummary("Get the latest character transform")
            .WithDescription("Returns the most recently persisted world position and horizontal rotation for the authenticated user's character.")
            .WithResponseDescription(StatusCodes.Status200OK, "The latest character transform was found and returned.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(Policies.Client);

        groupBuilder
            .MapPost(SaveCharacterTransform)
            .WithSummary("Save a character transform")
            .WithDescription("Persists a new world-position snapshot for the authenticated user's character.")
            .WithRequestBodyDescription("World position and horizontal rotation to persist.")
            .WithResponseDescription(StatusCodes.Status201Created, "The transform snapshot was persisted.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(Policies.Server);
    }

    private static async Task<Ok<CharacterTransformDto>> GetCharacterTransform(
        ISender sender,
        [AsParameters] GetCharacterTransformQuery query,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(query, cancellationToken);

        return TypedResults.Ok(result);
    }

    private static async Task<Created> SaveCharacterTransform(
        ISender sender,
        SaveCharacterTransformCommand command,
        CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);

        return TypedResults.Created("/api/CharacterTransforms");
    }   
}
