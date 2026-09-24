using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using ProjectX.API.Infrastructure;
using ProjectX.Application.CharacterStashes;

namespace ProjectX.API.Endpoints;

public class CharacterStashes : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(AccessCharacterStash)
            .WithSummary("Access the account stash")
            .WithDescription("Reads or atomically transfers, moves or splits stacks for the session-bound character and account; updates Collect progress.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthorizationPolicies.ServerPlayerSession);
    }

    public static async Task<Ok<CharacterStashDto>> AccessCharacterStash(ISender sender, AccessCharacterStashCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return TypedResults.Ok(result);
    }
}
