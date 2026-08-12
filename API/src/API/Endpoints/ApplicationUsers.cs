using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using ProjectX.API.Infrastructure;
using ProjectX.Application.ApplicationUsers.Commands.LoginApplicationUser;
using ProjectX.Application.ApplicationUsers.Commands.RefreshSession;

namespace ProjectX.API.Endpoints;

public class ApplicationUsers : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder
            .MapPost(LoginAsync)
            .WithSummary("Log in")
            .WithDescription("Authenticates a user and returns an access token.")
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status429TooManyRequests)
            .ProducesValidationProblem()
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitPolicies.Login);

        groupBuilder
            .MapPost(RefreshSessionAsync, "RefreshSession")
            .WithSummary("Refresh session")
            .WithDescription("Issues a replacement access token for an eligible session.")
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization(AuthorizationPolicies.ServerOrClient);
    }

    public static async Task<Ok<LoginApplicationUserDto>> LoginAsync(
        ISender sender,
        LoginApplicationUserCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return TypedResults.Ok(result);
    }

    public static async Task<Ok<RefreshSessionDto>> RefreshSessionAsync(
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RefreshSessionCommand(), cancellationToken);

        return TypedResults.Ok(result);
    }
}
