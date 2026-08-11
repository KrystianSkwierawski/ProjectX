using MediatR;
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
            .WithSummary("Register a dedicated game server session")
            .WithDescription("Registers the dedicated server's current direct or Relay allocation and starts a short UTC lease. A new registration replaces the previous session owned by the same server account.")
            .WithRequestBodyDescription("Transport mode and Relay join code. The join code is required only for Relay sessions.")
            .WithResponseDescription(StatusCodes.Status200OK, "The game session was registered and its lease expiry was returned.")
            .WithResponseDescription(StatusCodes.Status403Forbidden, "Direct transport is disabled in this environment, or the authenticated principal is not a server.")
            .Produces<RegisterGameSessionDto>()
            .Produces(StatusCodes.Status403Forbidden)
            .ProducesValidationProblem()
            .RequireAuthorization(AuthorizationPolicies.Server);

        groupBuilder
            .MapPost(HeartbeatAsync, "Heartbeat")
            .WithSummary("Renew a dedicated game server session lease")
            .WithDescription("Renews an active game-session lease from the current UTC time. An expired lease cannot be revived and the server must register a new session.")
            .WithRequestBodyDescription("The active game-session identifier assigned to this dedicated server.")
            .WithResponseDescription(StatusCodes.Status200OK, "The game-session lease was renewed and its new UTC expiry was returned.")
            .WithResponseDescription(StatusCodes.Status401Unauthorized, "The game session is invalid, expired, or belongs to another server account.")
            .Produces<HeartbeatGameSessionDto>()
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesValidationProblem()
            .RequireAuthorization(AuthorizationPolicies.Server);

        groupBuilder
            .MapPost(CreateTicketAsync, "Ticket")
            .WithSummary("Create a one-time game connection ticket")
            .WithDescription("Returns the active server's connection information and a random ticket that expires quickly and can be redeemed exactly once. Creating another ticket for the same client and game session replaces the previous ticket.")
            .WithResponseDescription(StatusCodes.Status200OK, "A one-time connection ticket was created.")
            .WithResponseDescription(StatusCodes.Status404NotFound, "No active game server session is registered.")
            .WithResponseDescription(StatusCodes.Status429TooManyRequests, "The per-account ticket creation limit of 20 requests per minute was exceeded.")
            .Produces<CreateGameSessionTicketDto>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status429TooManyRequests)
            .RequireAuthorization(AuthorizationPolicies.Client)
            .RequireRateLimiting(RateLimitPolicies.GameSessionTicket);

        groupBuilder
            .MapPost(RedeemTicketAsync, "Redeem")
            .WithSummary("Redeem a one-time game connection ticket")
            .WithDescription("Atomically consumes a ticket assigned to this server session and creates a server-only player-session credential.")
            .WithRequestBodyDescription("Game-session identifier and one-time connection ticket received during NGO connection approval.")
            .WithResponseDescription(StatusCodes.Status200OK, "The ticket was consumed and a player-session credential was issued.")
            .WithResponseDescription(StatusCodes.Status401Unauthorized, "The ticket is invalid, expired, already consumed, or belongs to another server session.")
            .Produces<RedeemGameSessionTicketDto>()
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesValidationProblem()
            .RequireAuthorization(AuthorizationPolicies.Server);

        groupBuilder
            .MapPost(RevokePlayerAsync, "RevokePlayer")
            .WithSummary("Revoke a disconnected player's server session")
            .WithDescription("Revokes the server-only player-session credential after the corresponding Netcode client disconnects.")
            .WithRequestBodyDescription("The server-only player-session credential to revoke.")
            .WithResponseDescription(StatusCodes.Status204NoContent, "The credential was revoked or was already unavailable.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .RequireAuthorization(AuthorizationPolicies.Server);
    }

    private static async Task<IResult> RegisterAsync(ISender sender, RegisterGameSessionCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return TypedResults.Ok(result);
    }

    private static async Task<IResult> CreateTicketAsync(ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateGameSessionTicketCommand(), cancellationToken);

        return TypedResults.Ok(result);
    }

    private static async Task<IResult> HeartbeatAsync(ISender sender, HeartbeatGameSessionCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return TypedResults.Ok(result);
    }

    private static async Task<IResult> RedeemTicketAsync(ISender sender, RedeemGameSessionTicketCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return TypedResults.Ok(result);
    }

    private static async Task<IResult> RevokePlayerAsync(ISender sender, RevokePlayerSessionCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);

        return TypedResults.NoContent();
    }
}
