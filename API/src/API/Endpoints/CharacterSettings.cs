using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using ProjectX.API.Infrastructure;
using ProjectX.Application.CharacterSettings;
using ProjectX.Application.CharacterSettings.Commands.UpdateCharacterSettings;
using ProjectX.Application.CharacterSettings.Queries.GetCharacterSettings;

namespace ProjectX.API.Endpoints;

public class CharacterSettings : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetCharacterSettings)
            .WithSummary("Get character settings")
            .WithDescription("Returns the owned character's language and ten action bar bindings in key order 1 through 0.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthorizationPolicies.Client);

        groupBuilder.MapPost(UpdateCharacterSettings)
            .WithSummary("Update character settings")
            .WithDescription("Saves language and exactly ten usable item bindings for an owned character. None clears a slot.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthorizationPolicies.Client);
    }

    public static async Task<Ok<CharacterSettingsDto>> GetCharacterSettings(
        ISender sender, [AsParameters] GetCharacterSettingsQuery query, CancellationToken cancellationToken)
    {
        var result = await sender.Send(query, cancellationToken);

        return TypedResults.Ok(result);
    }

    public static async Task<Ok<CharacterSettingsDto>> UpdateCharacterSettings(
        ISender sender, UpdateCharacterSettingsCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return TypedResults.Ok(result);
    }
}
