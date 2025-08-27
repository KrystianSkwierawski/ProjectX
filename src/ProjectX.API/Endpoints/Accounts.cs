using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using ProjectX.API.Infrastructure;
using ProjectX.Application.Common;
using ProjectX.Infrastructure.Identity;

namespace ProjectX.API.Endpoints;

public class Accounts : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(LoginAsync);
    }

    private static async Task<Results<Ok<string>, BadRequest>> LoginAsync(JwtHandler jwtHandler, UserManager<ApplicationUser> userManager)
    {
        var user = await userManager.FindByEmailAsync("administrator@localhost") ?? throw new Exception("not found user");

        if (await userManager.CheckPasswordAsync(user, "Administrator1!"))
        {
            var claims = jwtHandler.GetClamis(user.Email, user.Id);
            string token = jwtHandler.GenerateToken(claims);

            return TypedResults.Ok(token);
        }

        return TypedResults.BadRequest();
    }
}
