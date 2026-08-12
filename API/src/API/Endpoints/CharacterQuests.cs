using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using ProjectX.API.Infrastructure;
using ProjectX.Application.CharacterQuests.Commands.AcceptCharacterQuest;
using ProjectX.Application.CharacterQuests.Commands.AddCharacterQuestProgress;
using ProjectX.Application.CharacterQuests.Commands.CheckCharacterQuestProgress;
using ProjectX.Application.CharacterQuests.Commands.CompleteCharacterQuest;
using ProjectX.Application.CharacterQuests.Queries.GetCharacterQuests;

namespace ProjectX.API.Endpoints;

public class CharacterQuests : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder
            .MapGet(GetCharacterQuests)
            .WithSummary("Get character quests")
            .WithDescription("Returns quests assigned to a character.")
            .RequireAuthorization(AuthorizationPolicies.Client);

        groupBuilder
            .MapPost(AcceptCharacterQuest, "Accept")
            .WithSummary("Accept quest")
            .WithDescription("Assigns a quest to the authenticated user's character.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthorizationPolicies.Client);

        groupBuilder
            .MapPost(AddCharacterQuestProgress, "Progress")
            .WithSummary("Add quest progress")
            .WithDescription("Adds progress to an accepted character quest.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthorizationPolicies.ServerPlayerSession);

        groupBuilder
            .MapPost(CheckCharacterQuestProgress, "CheckProgress")
            .WithSummary("Check quest progress")
            .WithDescription("Applies progress to a matching active quest.")
            .RequireAuthorization(AuthorizationPolicies.ServerPlayerSession);

        groupBuilder
            .MapPost(CompleteCharacterQuest, "Complete")
            .WithSummary("Complete quest")
            .WithDescription("Completes a finished quest and returns its reward.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthorizationPolicies.ServerPlayerSession);
    }

    public static async Task<Ok<GetCharacterQuestsDto>> GetCharacterQuests(
        ISender sender,
        [AsParameters] GetCharacterQuestsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(query, cancellationToken);

        return TypedResults.Ok(result);
    }

    public static async Task<Created<CharacterQuestDto>> AcceptCharacterQuest(
        ISender sender,
        AcceptCharacterQuestCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return TypedResults.Created("/api/CharacterQuests", result);
    }

    public static async Task<Ok<AddCharacterQuestProgressDto>> AddCharacterQuestProgress(
        ISender sender,
        AddCharacterQuestProgressCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return TypedResults.Ok(result);
    }

    public static async Task<Ok<CheckCharacterQuestProgressDto>> CheckCharacterQuestProgress(
        ISender sender,
        CheckCharacterQuestProgressCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return TypedResults.Ok(result);
    }

    public static async Task<Ok<CompleteCharacterQuestDto>> CompleteCharacterQuest(
        ISender sender,
        CompleteCharacterQuestCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return TypedResults.Ok(result);
    }
}
