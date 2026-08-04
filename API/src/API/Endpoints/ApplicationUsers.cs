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
            .Produces<LoginApplicationUserDto>()
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesValidationProblem();
    }

    private static async Task<IResult> LoginAsync(ISender sender, LoginApplicationUserCommand command)
    {
        try
        {
            var result = await sender.Send(command);

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
