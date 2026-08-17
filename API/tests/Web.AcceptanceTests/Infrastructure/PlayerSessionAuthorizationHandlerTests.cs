using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Moq;
using ProjectX.API.Infrastructure;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Application.Common.Security;
using ProjectX.Application.GameSessions.Models;

namespace ProjectX.Web.AcceptanceTests.Infrastructure;

public sealed class PlayerSessionAuthorizationHandlerTests
{
    private const string ServerUserId = "server-user";
    private const string ClientUserId = "client-user";
    private const string PlayerSessionId = "player-session-secret";
    private const int CharacterId = 42;

    [Fact]
    public async Task HandleAsync_ValidServerScopedSession_SucceedsAndSetsDelegatedUser()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[PlayerSessionAuthorizationHandler.HeaderName] = PlayerSessionId;
        var service = new Mock<IGameSessionService>();
        var resolvedPlayerSession = new ResolvedPlayerSession(ClientUserId, CharacterId);

        service.Setup(x => x.TryResolvePlayer(ServerUserId, PlayerSessionId, out resolvedPlayerSession)).Returns(true);

        var authorizationContext = CreateAuthorizationContext();
        var handler = CreateHandler(httpContext, service.Object);

        await handler.HandleAsync(authorizationContext);

        Assert.True(authorizationContext.HasSucceeded);
        Assert.Equal(ClientUserId, httpContext.Items[PlayerSessionAuthorizationHandler.DelegatedUserIdItemKey]);
        Assert.Equal(CharacterId, httpContext.Items[PlayerSessionAuthorizationHandler.DelegatedCharacterIdItemKey]);
    }

    [Fact]
    public async Task HandleAsync_MissingPlayerSessionHeader_DoesNotAuthorize()
    {
        var httpContext = new DefaultHttpContext();
        var service = new Mock<IGameSessionService>();
        var authorizationContext = CreateAuthorizationContext();
        var handler = CreateHandler(httpContext, service.Object);

        await handler.HandleAsync(authorizationContext);

        Assert.False(authorizationContext.HasSucceeded);
        Assert.False(httpContext.Items.ContainsKey(PlayerSessionAuthorizationHandler.DelegatedUserIdItemKey));
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_SessionNotBoundToAuthenticatedServer_DoesNotAuthorize()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[PlayerSessionAuthorizationHandler.HeaderName] = PlayerSessionId;
        var service = new Mock<IGameSessionService>();
        var ignoredPlayerSession = new ResolvedPlayerSession(string.Empty, default);

        service.Setup(x => x.TryResolvePlayer(ServerUserId, PlayerSessionId, out ignoredPlayerSession)).Returns(false);

        var authorizationContext = CreateAuthorizationContext();
        var handler = CreateHandler(httpContext, service.Object);

        await handler.HandleAsync(authorizationContext);

        Assert.False(authorizationContext.HasSucceeded);
        Assert.False(httpContext.Items.ContainsKey(PlayerSessionAuthorizationHandler.DelegatedUserIdItemKey));
        Assert.False(httpContext.Items.ContainsKey(PlayerSessionAuthorizationHandler.DelegatedCharacterIdItemKey));
    }

    private static PlayerSessionAuthorizationHandler CreateHandler(HttpContext httpContext, IGameSessionService service)
    {
        return new PlayerSessionAuthorizationHandler(new HttpContextAccessor { HttpContext = httpContext }, service);
    }

    private static AuthorizationHandlerContext CreateAuthorizationContext()
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, ServerUserId),
            new Claim(ClaimTypes.Role, ApplicationRoles.Server)
        ], "Bearer");

        return new AuthorizationHandlerContext([new PlayerSessionAuthorizationRequirement()], new ClaimsPrincipal(identity), resource: null);
    }
}
