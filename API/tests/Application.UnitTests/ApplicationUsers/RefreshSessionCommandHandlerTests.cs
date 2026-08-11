using Moq;
using ProjectX.Application.ApplicationUsers.Commands.RefreshSession;
using ProjectX.Application.Common.Exceptions;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Application.Common.Models;
using ProjectX.Application.Common.Security;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.UnitTests.ApplicationUsers;

public class RefreshSessionCommandHandlerTests
{
    private const string UserId = "user-id";

    [Fact]
    public async Task Handle_IssuesFreshTokenForAuthenticatedUser()
    {
        var user = CreateUser();
        var timeProvider = new ManualTimeProvider(new DateTimeOffset(2026, 8, 10, 12, 0, 0, TimeSpan.Zero));
        var currentUser = CreateCurrentUserService(timeProvider.GetUtcNow().Add(SessionTokenPolicy.RefreshWindow));
        var authentication = CreateAuthenticationService(user);
        var accessTokens = new Mock<IAccessTokenService>();
        accessTokens.Setup(service => service.Create(user)).Returns("renewed-jwt");
        var handler = new RefreshSessionCommandHandler(currentUser.Object, authentication.Object, accessTokens.Object, timeProvider);

        var result = await handler.Handle(new RefreshSessionCommand(), CancellationToken.None);

        Assert.Equal("renewed-jwt", result.Token);
        Assert.Equal(LanguageEnum.pl, result.Language);
        accessTokens.Verify(service => service.Create(user), Times.Once);
    }

    [Fact]
    public async Task Handle_RejectsRefreshBeforeFinalFiveMinutes()
    {
        var timeProvider = new ManualTimeProvider(new DateTimeOffset(2026, 8, 10, 12, 0, 0, TimeSpan.Zero));
        var currentUser = CreateCurrentUserService(timeProvider.GetUtcNow().Add(SessionTokenPolicy.Lifetime));
        var authentication = CreateAuthenticationService(CreateUser());
        var accessTokens = new Mock<IAccessTokenService>();
        var handler = new RefreshSessionCommandHandler(currentUser.Object, authentication.Object, accessTokens.Object, timeProvider);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => handler.Handle(new RefreshSessionCommand(), CancellationToken.None));
        accessTokens.Verify(service => service.Create(It.IsAny<AuthenticatedApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RejectsTokenWithoutExpirationClaim()
    {
        var currentUser = CreateCurrentUserService(null);
        var authentication = CreateAuthenticationService(CreateUser());
        var accessTokens = new Mock<IAccessTokenService>();
        var handler = new RefreshSessionCommandHandler(currentUser.Object, authentication.Object, accessTokens.Object, TimeProvider.System);

        await Assert.ThrowsAsync<InvalidCredentialsException>(() => handler.Handle(new RefreshSessionCommand(), CancellationToken.None));
        accessTokens.Verify(service => service.Create(It.IsAny<AuthenticatedApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RejectsUnavailableUser()
    {
        var currentUser = CreateCurrentUserService(DateTimeOffset.UtcNow.AddMinutes(5));
        var authentication = CreateAuthenticationService(null);
        var tokenService = new Mock<IAccessTokenService>();
        var handler = new RefreshSessionCommandHandler(currentUser.Object, authentication.Object, tokenService.Object, TimeProvider.System);

        await Assert.ThrowsAsync<InvalidCredentialsException>(() => handler.Handle(new RefreshSessionCommand(), CancellationToken.None));
        tokenService.Verify(service => service.Create(It.IsAny<AuthenticatedApplicationUser>()), Times.Never);
    }

    private static Mock<ICurrentUserService> CreateCurrentUserService(DateTimeOffset? expiration)
    {
        var service = new Mock<ICurrentUserService>();
        service.Setup(current => current.GetAuthenticatedUserId()).Returns(UserId);
        service.Setup(current => current.GetAuthenticatedTokenExpirationUtc()).Returns(expiration);
        return service;
    }

    private static Mock<IApplicationUserAuthenticationService> CreateAuthenticationService(AuthenticatedApplicationUser? user)
    {
        var service = new Mock<IApplicationUserAuthenticationService>();
        service.Setup(authentication => authentication.FindActiveByIdAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        return service;
    }

    private static AuthenticatedApplicationUser CreateUser()
    {
        return new AuthenticatedApplicationUser(UserId, "user@example.com", "user@example.com", LanguageEnum.pl, [ApplicationRoles.Server]);
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;

        public ManualTimeProvider(DateTimeOffset utcNow) => _utcNow = utcNow;
        public override DateTimeOffset GetUtcNow() => _utcNow;
        public void Advance(TimeSpan duration) => _utcNow = _utcNow.Add(duration);
    }
}
