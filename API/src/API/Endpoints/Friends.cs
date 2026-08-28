using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using ProjectX.API.Infrastructure;
using ProjectX.Application.Friends.Commands.RemoveFriend;
using ProjectX.Application.Friends.Commands.RespondFriendInvitation;
using ProjectX.Application.Friends.Commands.SendFriendInvitation;
using ProjectX.Application.Friends.Queries.AuthorizeWhisper;
using ProjectX.Application.Friends.Queries.GetFriendList;

namespace ProjectX.API.Endpoints;

public class Friends : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder
            .MapGet(GetFriendList)
            .WithSummary("Get friend list")
            .WithDescription("Returns friends and pending invitations for the selected character.")
            .RequireAuthorization(AuthorizationPolicies.ServerPlayerSession);

        groupBuilder
            .MapPost(SendFriendInvitation, "Invitations")
            .WithSummary("Send friend invitation")
            .WithDescription("Invites another active character by name.")
            .ProducesValidationProblem()
            .RequireAuthorization(AuthorizationPolicies.ServerPlayerSession);

        groupBuilder
            .MapPost(RespondFriendInvitation, "Invitations/Respond")
            .WithSummary("Respond to friend invitation")
            .WithDescription("Accepts or declines an incoming friendship invitation.")
            .ProducesValidationProblem()
            .RequireAuthorization(AuthorizationPolicies.ServerPlayerSession);

        groupBuilder
            .MapDelete(RemoveFriend, "{characterId:int}")
            .WithSummary("Remove friend")
            .WithDescription("Removes an accepted friendship for the selected character.")
            .ProducesValidationProblem()
            .RequireAuthorization(AuthorizationPolicies.ServerPlayerSession);

        groupBuilder
            .MapGet(AuthorizeWhisper, "{characterId:int}/WhisperAuthorization")
            .WithSummary("Authorize whisper")
            .WithDescription("Checks whether the selected character may whisper the target friend.")
            .ProducesValidationProblem()
            .RequireAuthorization(AuthorizationPolicies.ServerPlayerSession);
    }

    public static async Task<Ok<GetFriendListDto>> GetFriendList(ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetFriendListQuery(), cancellationToken);

        return TypedResults.Ok(result);
    }

    public static async Task<Ok<SendFriendInvitationDto>> SendFriendInvitation(
        ISender sender,
        SendFriendInvitationCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return TypedResults.Ok(result);
    }

    public static async Task<Ok<RespondFriendInvitationDto>> RespondFriendInvitation(
        ISender sender,
        RespondFriendInvitationCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return TypedResults.Ok(result);
    }

    public static async Task<Ok<RemoveFriendDto>> RemoveFriend(
        ISender sender,
        int characterId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RemoveFriendCommand(characterId), cancellationToken);

        return TypedResults.Ok(result);
    }

    public static async Task<Ok<AuthorizeWhisperDto>> AuthorizeWhisper(
        ISender sender,
        int characterId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AuthorizeWhisperQuery(characterId), cancellationToken);

        return TypedResults.Ok(result);
    }
}
