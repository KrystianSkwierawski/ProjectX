using MediatR;
using ProjectX.API.Infrastructure;
using ProjectX.Application.ApplicationUsers.Commands.LoginApplicationUser;
using ProjectX.Application.Common.Exceptions;

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
    }

    private static async Task<IResult> LoginAsync(
        ISender sender,
        LoginApplicationUserCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await sender.Send(command, cancellationToken);

            return TypedResults.Ok(result);
        }
        catch (InvalidCredentialsException)
        {
            return TypedResults.Unauthorized();
        }
        catch (ValidationException exception)
        {
            return TypedResults.ValidationProblem(exception.Errors);
        }
    }
}
