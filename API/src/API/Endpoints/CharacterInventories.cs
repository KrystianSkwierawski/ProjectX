using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using ProjectX.API.Infrastructure;
using ProjectX.Application.CharacterInventories.Commands.ResolveCharacterInventoryTrade;
using ProjectX.Application.CharacterInventories.Commands.TradeCharacterInventories;
using ProjectX.Application.CharacterInventories.Commands.UpdateCharacterInventory;
using ProjectX.Application.CharacterInventories.Queries.GetCharacterInventory;

namespace ProjectX.API.Endpoints;

public class CharacterInventories : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder
            .MapGet(GetCharacterInventory)
            .WithSummary("Get character inventory")
            .WithDescription("Returns a character's inventory and capacity.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthorizationPolicies.Client);

        groupBuilder
            .MapPost(UpdateCharacterInventory)
            .WithSummary("Update character inventory")
            .WithDescription("Atomically applies inventory changes and reports when the resulting items do not fit.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthorizationPolicies.ServerPlayerSession);

        groupBuilder
            .MapPost(TradeCharacterInventories, "Trade")
            .WithSummary("Trade character inventories")
            .WithDescription("Atomically exchanges two locked player offers after validating both inventories and capacities.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthorizationPolicies.ServerPlayerSession);

        groupBuilder
            .MapPost(ResolveCharacterInventoryTrade, "Trade/Resolve")
            .WithSummary("Resolve character inventory trade")
            .WithDescription("Returns authoritative inventories when a durable trade receipt confirms that an uncertain commit was applied.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthorizationPolicies.Server);
    }

    public static async Task<Ok<CharacterInventoryDto>> GetCharacterInventory(
        ISender sender,
        [AsParameters] GetCharacterInventoryQuery query,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(query, cancellationToken);

        return TypedResults.Ok(result);
    }

    public static async Task<Ok<UpdateCharacterInventoryDto>> UpdateCharacterInventory(
        ISender sender,
        UpdateCharacterInventoryCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return TypedResults.Ok(result);
    }

    public static async Task<Ok<TradeCharacterInventoriesDto>> TradeCharacterInventories(
        ISender sender,
        TradeCharacterInventoriesCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return TypedResults.Ok(result);
    }

    public static async Task<Ok<TradeCharacterInventoriesDto>> ResolveCharacterInventoryTrade(
        ISender sender,
        ResolveCharacterInventoryTradeCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return TypedResults.Ok(result);
    }
}
