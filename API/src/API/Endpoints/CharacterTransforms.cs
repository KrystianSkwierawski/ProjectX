using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using ProjectX.API.Infrastructure;
using ProjectX.Application.CharacterTransforms.Commands.SaveCharacterTransform;
using ProjectX.Application.CharacterTransforms.Queries.GetCharacterTransform;

namespace ProjectX.API.Endpoints;

public class CharacterTransforms : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder
            .MapGet(GetCharacterTransform)
            .WithSummary("Get character transform")
            .WithDescription("Returns the latest saved character transform.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthorizationPolicies.Client);

        groupBuilder
            .MapPost(SaveCharacterTransform)
            .WithSummary("Save character transform")
            .WithDescription("Saves a character's world transform.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthorizationPolicies.ServerPlayerSession);
    }

    public static async Task<Ok<CharacterTransformDto>> GetCharacterTransform(
        ISender sender,
        [AsParameters] GetCharacterTransformQuery query,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(query, cancellationToken);

        return TypedResults.Ok(result);
    }

    public static async Task<Created> SaveCharacterTransform(
        ISender sender,
        SaveCharacterTransformCommand command,
        CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);

        return TypedResults.Created("/api/CharacterTransforms");
    }
}
