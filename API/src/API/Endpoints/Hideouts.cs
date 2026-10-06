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
            .WithSummary("Read and manage the character's hideout")
            .WithDescription("Uses the session-bound character. Reads reconcile elapsed growth and upgrade deadlines. Construction, upgrades, seed transfers and harvests atomically persist building state, inventory and active Collect progress. Mutations require the current farm revision.")
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
