using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using ProjectX.API.Infrastructure;
using ProjectX.Application.CharacterExperiences.Commands.AddCharacterExperience;
using ProjectX.Domain.Constants;

namespace ProjectX.API.Endpoints;

public class CharacterExperiences : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder
            .MapPost(AddCharacterExperience)
            .WithSummary("Add character experience")
            .WithDescription("Adds experience to one progression category and returns the updated total experience and calculated level.")
            .WithRequestBodyDescription("Character identifier, experience amount, and progression category.")
            .WithResponseDescription(StatusCodes.Status200OK, "Experience was added and the updated progression values are returned.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(Policies.Server);
    }

    private static async Task<Ok<AddCharacterExperienceDto>> AddCharacterExperience(
        ISender sender,
        AddCharacterExperienceCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return TypedResults.Ok(result);
    }
}
