using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using ProjectX.API.Infrastructure;
using ProjectX.Application.CharacterQuests.Commands.AcceptCharacterQuest;
using ProjectX.Application.CharacterQuests.Commands.AddCharacterQuestProgres;
using ProjectX.Application.CharacterQuests.Queries.GetCharacterQuests;
using ProjectX.Domain.Constants;

namespace ProjectX.API.Endpoints;

public class CharacterQuests : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetCharacterQuests).RequireAuthorization(Policies.Client);
        groupBuilder.MapPost(AcceptCharacterQuest).RequireAuthorization(Policies.Client);
        groupBuilder.MapPost(AddCharacterQuestProgress, "Progres").RequireAuthorization(Policies.Server);
    }

    private static async Task<Ok<GetCharacterQuestsDto>> GetCharacterQuests(ISender sender, [AsParameters] GetCharacterQuestsQuery query)
    {
        var result = await sender.Send(query);

        return TypedResults.Ok(result);
    }

    private static async Task<Created<int>> AcceptCharacterQuest(ISender sender, AcceptCharacterQuestCommand command)
    {
        var result = await sender.Send(command);

        return TypedResults.Created(string.Empty, result);
    }

    private static async Task<Ok<AddCharacterQuestProgresDto>> AddCharacterQuestProgress(ISender sender, AddCharacterQuestProgresCommand command)
    {
        var result = await sender.Send(command);

        return TypedResults.Ok(result);
    }
}
