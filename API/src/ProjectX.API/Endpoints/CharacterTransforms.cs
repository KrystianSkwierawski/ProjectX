using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using ProjectX.API.Infrastructure;
using ProjectX.Application.CharacterTransforms.Commands.SaveCharacterTransform;
using ProjectX.Application.CharacterTransforms.Queries.GetCharacterTransform;
using ProjectX.Domain.Constants;

namespace ProjectX.API.Endpoints;

public class CharacterTransforms : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetAsync).RequireAuthorization(Policies.Client);
        groupBuilder.MapPost(SaveAsync).RequireAuthorization(Policies.Server);
    }

    private static async Task<Ok<CharacterTransformDto>> GetAsync(ISender sender, [AsParameters] GetCharacterTransformQuery query)
    {
        var result = await sender.Send(query);

        return TypedResults.Ok(result);
    }

    private static async Task<NoContent> SaveAsync(ISender sender, SaveTransformTransformCommand command)
    {
        await sender.Send(command);

        return TypedResults.NoContent();
    }   
}
