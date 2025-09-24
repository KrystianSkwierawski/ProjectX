using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using ProjectX.API.Infrastructure;
using ProjectX.Application.ApplicationUsers.Commands.LoginApplicationUser;

namespace ProjectX.API.Endpoints;

public class ApplicationUsers : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(LoginAsync);
    }

    private static async Task<Ok<LoginApplicationUserDto>> LoginAsync(ISender sender, LoginApplicationUserCommand command)
    {
        var result = await sender.Send(command);

        return TypedResults.Ok(result);
    }
}
