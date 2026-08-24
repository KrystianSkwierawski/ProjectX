using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using ProjectX.API.Infrastructure;
using ProjectX.Application.CharacterExperiences.Commands.AddCharacterExperience;

namespace ProjectX.API.Endpoints;

public class CharacterExperiences : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder
            .MapPost(AddCharacterExperience)
            .WithSummary("Add character experience")
            .WithDescription("Adds experience and returns the updated progression.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthorizationPolicies.ServerPlayerSession);
    }

    public static async Task<Ok<AddCharacterExperienceDto>> AddCharacterExperience(
        ISender sender,
        AddCharacterExperienceCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return TypedResults.Ok(result);
    }
}
