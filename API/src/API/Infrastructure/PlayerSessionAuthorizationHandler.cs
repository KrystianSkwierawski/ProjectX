using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using ProjectX.Application.Common.Interfaces;

namespace ProjectX.API.Infrastructure;

public sealed class PlayerSessionAuthorizationHandler : AuthorizationHandler<PlayerSessionAuthorizationRequirement>
{
    public const string DelegatedCharacterIdItemKey = "ProjectX.DelegatedCharacterId";
    public const string DelegatedUserIdItemKey = "ProjectX.DelegatedUserId";
    public const string HeaderName = "PlayerSessionId";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IGameSessionService _gameSessionService;

    public PlayerSessionAuthorizationHandler(IHttpContextAccessor httpContextAccessor, IGameSessionService gameSessionService)
    {
        _httpContextAccessor = httpContextAccessor;
        _gameSessionService = gameSessionService;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PlayerSessionAuthorizationRequirement requirement)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var serverUserId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var playerSessionId = httpContext?.Request.Headers[HeaderName].ToString();

        if (!string.IsNullOrWhiteSpace(serverUserId)
            && !string.IsNullOrWhiteSpace(playerSessionId)
            && _gameSessionService.TryResolvePlayer(serverUserId, playerSessionId, out var playerSession))
        {
            httpContext!.Items[DelegatedUserIdItemKey] = playerSession.UserId;
            httpContext.Items[DelegatedCharacterIdItemKey] = playerSession.CharacterId;
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
