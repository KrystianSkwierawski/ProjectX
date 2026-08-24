using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using ProjectX.API.Infrastructure;
using ProjectX.Application.GameSessions.Commands.CreateGameSessionTicket;
using ProjectX.Application.GameSessions.Commands.HeartbeatGameSession;
using ProjectX.Application.GameSessions.Commands.RedeemGameSessionTicket;
using ProjectX.Application.GameSessions.Commands.RegisterGameSession;
using ProjectX.Application.GameSessions.Commands.RevokePlayerSession;

namespace ProjectX.API.Endpoints;

public class GameSessions : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder
            .MapPost(RegisterAsync, "Register")
            .WithSummary("Register game session")
            .WithDescription("Registers a dedicated game server session.")
            .Produces(StatusCodes.Status403Forbidden)
            .ProducesValidationProblem()
            .RequireAuthorization(AuthorizationPolicies.Server);

        groupBuilder
            .MapPost(HeartbeatAsync, "Heartbeat")
            .WithSummary("Renew game session")
            .WithDescription("Renews an active game server lease.")
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesValidationProblem()
            .RequireAuthorization(AuthorizationPolicies.Server);

        groupBuilder
            .MapPost(CreateTicketAsync, "Ticket")
            .WithSummary("Create connection ticket")
            .WithDescription("Creates a short-lived one-time connection ticket for a selected character.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status429TooManyRequests)
            .RequireAuthorization(AuthorizationPolicies.Client)
            .RequireRateLimiting(RateLimitPolicies.GameSessionTicket);

        groupBuilder
            .MapPost(RedeemTicketAsync, "Redeem")
            .WithSummary("Redeem connection ticket")
            .WithDescription("Redeems a ticket and creates a player session.")
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesValidationProblem()
            .RequireAuthorization(AuthorizationPolicies.Server);

        groupBuilder
            .MapPost(RevokePlayerAsync, "RevokePlayer")
            .WithSummary("Revoke player session")
            .WithDescription("Revokes a disconnected player's session.")
            .ProducesValidationProblem()
            .RequireAuthorization(AuthorizationPolicies.Server);
    }

    public static async Task<Ok<RegisterGameSessionDto>> RegisterAsync(
        ISender sender,
        RegisterGameSessionCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return TypedResults.Ok(result);
    }

    public static async Task<Ok<CreateGameSessionTicketDto>> CreateTicketAsync(
        ISender sender,
        CreateGameSessionTicketCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return TypedResults.Ok(result);
    }

    public static async Task<Ok<HeartbeatGameSessionDto>> HeartbeatAsync(
        ISender sender,
        HeartbeatGameSessionCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return TypedResults.Ok(result);
    }

    public static async Task<Ok<RedeemGameSessionTicketDto>> RedeemTicketAsync(
        ISender sender,
        RedeemGameSessionTicketCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return TypedResults.Ok(result);
    }

    public static async Task<NoContent> RevokePlayerAsync(
        ISender sender,
        RevokePlayerSessionCommand command,
        CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);

        return TypedResults.NoContent();
    }
}
