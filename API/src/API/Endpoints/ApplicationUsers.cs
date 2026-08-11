using MediatR;
using ProjectX.API.Infrastructure;
using ProjectX.Application.ApplicationUsers.Commands.LoginApplicationUser;
using ProjectX.Application.ApplicationUsers.Commands.RefreshSession;
using ProjectX.Domain.Constants;

namespace ProjectX.API.Endpoints;

public class ApplicationUsers : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder
            .MapPost(LoginAsync)
            .WithSummary("Authenticate an application user")
            .WithDescription("Validates an email address and password, then returns a JWT bearer token and the user's preferred language.")
            .WithRequestBodyDescription("Application-user email address and password.")
            .WithResponseDescription(StatusCodes.Status200OK, "Authentication succeeded and a JWT bearer token was issued.")
            .WithResponseDescription(StatusCodes.Status400BadRequest, "The credentials payload failed validation.")
            .WithResponseDescription(StatusCodes.Status401Unauthorized, "The email or password is invalid, or the account is locked out.")
            .WithResponseDescription(StatusCodes.Status429TooManyRequests, "Too many login attempts were made from the same IP address.")
            .Produces<LoginApplicationUserDto>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status429TooManyRequests)
            .ProducesValidationProblem()
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitPolicies.Login);

        groupBuilder
            .MapPost(RefreshSessionAsync, "RefreshSession")
            .WithSummary("Refresh an authenticated session")
            .WithDescription("Issues a fresh one-hour JWT bearer token for the authenticated client or dedicated server during the current token's final five minutes. Refreshing does not revoke the current token, so it remains usable until its original expiry during the handover window.")
            .WithResponseDescription(StatusCodes.Status200OK, "The session was refreshed and a new JWT bearer token was issued.")
            .WithResponseDescription(StatusCodes.Status401Unauthorized, "The current JWT is invalid, expired, or belongs to an unavailable account.")
            .WithResponseDescription(StatusCodes.Status403Forbidden, "The current JWT is valid but is not yet inside its final five-minute refresh window, or the authenticated role cannot refresh sessions.")
            .Produces<RefreshSessionDto>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization(Policies.ServerOrClient);
    }

    private static async Task<IResult> LoginAsync(ISender sender, LoginApplicationUserCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return TypedResults.Ok(result);
    }

    private static async Task<IResult> RefreshSessionAsync(ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RefreshSessionCommand(), cancellationToken);

        return TypedResults.Ok(result);
    }
}
