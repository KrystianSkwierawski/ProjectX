using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using ProjectX.API.Infrastructure;
using ProjectX.Application.Hideouts;

namespace ProjectX.API.Endpoints;

public class Hideouts : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(AccessHideout)
            .WithSummary("Read or start construction in the character's hideout")
            .WithDescription("Uses the session-bound character. None reads the catalog and timestamps; a building ID atomically consumes its catalog materials and starts construction once.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthorizationPolicies.ServerPlayerSession);
    }

    public static async Task<Ok<HideoutDto>> AccessHideout(ISender sender, AccessHideoutCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return TypedResults.Ok(result);
    }
}
