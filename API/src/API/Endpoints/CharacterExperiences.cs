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
        groupBuilder.MapPost(AddCharacterExperience).RequireAuthorization(Policies.Server);
    }

    private static async Task<Ok<AddCharacterExperienceDto>> AddCharacterExperience(ISender sender, AddCharacterExperienceCommand command)
    {
        var result = await sender.Send(command);

        return TypedResults.Ok(result);
    }
}
