using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using ProjectX.API.Infrastructure;
using ProjectX.Application.CharacterExperiences.Commands.AddCharacterExperience;
using ProjectX.Application.CharacterQuests.Queries.GetCharacterQuest;
using ProjectX.Domain.Constants;

namespace ProjectX.API.Endpoints;

public class CharacterQuests : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetCharacterQuest).RequireAuthorization(Policies.Client);
        groupBuilder.MapPost(AddCharacterExperience).RequireAuthorization(Policies.Server);
    }

    private static async Task<Ok<CharacterQuestDto>> GetCharacterQuest(ISender sender, [AsParameters] GetCharacterQuestQuery query)
    {
        var result = await sender.Send(query);

        return TypedResults.Ok(result);
    }

    private static async Task<Ok<AddCharacterExperienceDto>> AddCharacterExperience(ISender sender, AddCharacterExperienceCommand command)
    {
        var result = await sender.Send(command);

        return TypedResults.Ok(result);
    }
}
