using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using ProjectX.API.Infrastructure;
using ProjectX.Application.CharacterQuests.Commands.AcceptCharacterQuest;
using ProjectX.Application.CharacterQuests.Commands.AddCharacterQuestProgress;
using ProjectX.Application.CharacterQuests.Commands.CheckCharacterQuestProgress;
using ProjectX.Application.CharacterQuests.Commands.CompleteCharacterQuest;
using ProjectX.Application.CharacterQuests.Queries.GetCharacterQuests;
using ProjectX.Domain.Constants;

namespace ProjectX.API.Endpoints;

public class CharacterQuests : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder
            .MapGet(GetCharacterQuests)
            .WithSummary("Get character quests")
            .WithDescription("Returns the quest lifecycle state and progress assigned to a character.")
            .WithParameterDescription("CharacterId", "Identifier of the character whose quests are requested.")
            .WithResponseDescription(StatusCodes.Status200OK, "Character quest states were returned.")
            .RequireAuthorization(Policies.Client);

        groupBuilder
            .MapPost(AcceptCharacterQuest, "Accept")
            .WithSummary("Accept a quest")
            .WithDescription("Creates an accepted quest state for the authenticated user's character.")
            .WithRequestBodyDescription("Identifier of the quest to accept.")
            .WithResponseDescription(StatusCodes.Status201Created, "The quest was accepted and its initial character state is returned.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(Policies.Client);

        groupBuilder
            .MapPost(AddCharacterQuestProgress, "Progress")
            .WithSummary("Add character quest progress")
            .WithDescription("Adds a progress increment to an accepted character quest and updates its lifecycle state when the requirement is met.")
            .WithRequestBodyDescription("Character quest identifier and progress increment.")
            .WithResponseDescription(StatusCodes.Status200OK, "Quest progress was updated and the resulting state is returned.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(Policies.Server);

        groupBuilder
            .MapPost(CheckCharacterQuestProgress, "CheckProgress")
            .WithSummary("Check and apply quest progress")
            .WithDescription("Finds an active quest matching a game-server event, applies progress when present, and returns the resulting state.")
            .WithRequestBodyDescription("Quest identifier, progress increment, and character identifier reported by the game server.")
            .WithResponseDescription(StatusCodes.Status200OK, "The matching active quest was updated, or an empty result is returned when none is active.")
            .ProducesValidationProblem()
            .RequireAuthorization(Policies.Server);

        groupBuilder
            .MapPost(CompleteCharacterQuest, "Complete")
            .WithSummary("Complete a finished character quest")
            .WithDescription("Marks a finished character quest as completed, consumes required collection items, and returns its reward.")
            .WithRequestBodyDescription("Identifier of the finished character quest to complete.")
            .WithResponseDescription(StatusCodes.Status200OK, "The quest was completed and its reward is returned.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(Policies.Server);
    }

    private static async Task<Ok<GetCharacterQuestsDto>> GetCharacterQuests(
        ISender sender,
        [AsParameters] GetCharacterQuestsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(query, cancellationToken);

        return TypedResults.Ok(result);
    }

    private static async Task<Created<CharacterQuestDto>> AcceptCharacterQuest(
        ISender sender,
        AcceptCharacterQuestCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return TypedResults.Created("/api/CharacterQuests", result);
    }

    private static async Task<Ok<AddCharacterQuestProgressDto>> AddCharacterQuestProgress(
        ISender sender,
        AddCharacterQuestProgressCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return TypedResults.Ok(result);
    }

    private static async Task<Ok<CheckCharacterQuestProgressDto>> CheckCharacterQuestProgress(
        ISender sender,
        CheckCharacterQuestProgressCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return TypedResults.Ok(result);
    }

    private static async Task<Ok<CompleteCharacterQuestDto>> CompleteCharacterQuest(
        ISender sender,
        CompleteCharacterQuestCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return TypedResults.Ok(result);
    }
}
