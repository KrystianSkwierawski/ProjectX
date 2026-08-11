using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Moq;
using ProjectX.Application.ApplicationUsers.Commands.RefreshSession;
using ProjectX.Application.Common;
using ProjectX.Application.Common.Exceptions;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Entities;
using ProjectX.Domain.Enums;
using JsonWebTokenHandler = Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler;

namespace ProjectX.UnitTests.Application.ApplicationUsers;

public class RefreshSessionCommandHandlerTests
{
    private const string _userId = "user-id";
    private const string _securityKey = "a-secure-test-key-that-is-at-least-32-characters-long";
    private const string _issuer = "ProjectX.UnitTests";
    private const string _audience = "ProjectX.UnitTests";

    [Fact]
    public async Task Handle_IssuesFreshTokenForAuthenticatedUser()
    {
        var user = CreateUser();
        var timeProvider = new ManualTimeProvider(new DateTimeOffset(2026, 8, 10, 12, 0, 0, TimeSpan.Zero));
        var originalTokenExpiresAtUtc = timeProvider.GetUtcNow().Add(JwtHandler.TokenLifetime);
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(service => service.GetAuthenticatedUserId()).Returns(_userId);
        currentUserService
            .Setup(service => service.GetAuthenticatedTokenExpirationUtc())
            .Returns(originalTokenExpiresAtUtc);
        var userManager = CreateUserManager();
        userManager.Setup(manager => manager.FindByIdAsync(_userId)).ReturnsAsync(user);
        userManager.Setup(manager => manager.IsLockedOutAsync(user)).ReturnsAsync(false);
        userManager.Setup(manager => manager.GetRolesAsync(user)).ReturnsAsync(["Server"]);
        var jwtHandler = CreateJwtHandler(userManager.Object, timeProvider);
        var handler = new RefreshSessionCommandHandler(currentUserService.Object, userManager.Object, jwtHandler, timeProvider);

        var originalToken = await jwtHandler.GenerateToken(user);
        timeProvider.Advance(TimeSpan.FromMinutes(55));
        var result = await handler.Handle(new RefreshSessionCommand(), CancellationToken.None);
        var nextResult = await handler.Handle(new RefreshSessionCommand(), CancellationToken.None);

        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = CreateValidationParameters(timeProvider);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        Assert.Equal(_userId, token.Claims.Single(claim => claim.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Contains(token.Claims, claim => claim.Type == ClaimTypes.Role && claim.Value == "Server");
        Assert.Equal(LanguageEnum.pl, result.Language);
        Assert.NotEqual(result.Token, nextResult.Token);

        tokenHandler.ValidateToken(originalToken, validationParameters, out _);
        tokenHandler.ValidateToken(result.Token, validationParameters, out _);
        var middlewareValidation = await new JsonWebTokenHandler()
            .ValidateTokenAsync(result.Token, validationParameters);
        Assert.True(middlewareValidation.IsValid, middlewareValidation.Exception?.ToString());

        timeProvider.Advance(TimeSpan.FromMinutes(4) + TimeSpan.FromSeconds(59));
        tokenHandler.ValidateToken(originalToken, validationParameters, out _);

        timeProvider.Advance(TimeSpan.FromSeconds(2));
        Assert.Throws<SecurityTokenInvalidLifetimeException>(() =>
            tokenHandler.ValidateToken(originalToken, validationParameters, out _));
        tokenHandler.ValidateToken(result.Token, validationParameters, out _);
    }

    [Fact]
    public async Task Handle_RejectsRefreshBeforeFinalFiveMinutes()
    {
        var user = CreateUser();
        var timeProvider = new ManualTimeProvider(new DateTimeOffset(2026, 8, 10, 12, 0, 0, TimeSpan.Zero));
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(service => service.GetAuthenticatedUserId()).Returns(_userId);
        currentUserService
            .Setup(service => service.GetAuthenticatedTokenExpirationUtc())
            .Returns(timeProvider.GetUtcNow().Add(JwtHandler.TokenLifetime));
        var userManager = CreateUserManager();
        userManager.Setup(manager => manager.FindByIdAsync(_userId)).ReturnsAsync(user);
        userManager.Setup(manager => manager.IsLockedOutAsync(user)).ReturnsAsync(false);
        var handler = new RefreshSessionCommandHandler(currentUserService.Object, userManager.Object, CreateJwtHandler(userManager.Object, timeProvider), timeProvider);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => handler.Handle(new RefreshSessionCommand(), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_RejectsTokenWithoutExpirationClaim()
    {
        var user = CreateUser();
        var timeProvider = new ManualTimeProvider(new DateTimeOffset(2026, 8, 10, 12, 0, 0, TimeSpan.Zero));
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(service => service.GetAuthenticatedUserId()).Returns(_userId);
        var userManager = CreateUserManager();
        userManager.Setup(manager => manager.FindByIdAsync(_userId)).ReturnsAsync(user);
        userManager.Setup(manager => manager.IsLockedOutAsync(user)).ReturnsAsync(false);
        var handler = new RefreshSessionCommandHandler(currentUserService.Object, userManager.Object, CreateJwtHandler(userManager.Object, timeProvider), timeProvider);

        await Assert.ThrowsAsync<InvalidCredentialsException>(() => handler.Handle(new RefreshSessionCommand(), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_RejectsUnavailableUser()
    {
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(service => service.GetAuthenticatedUserId()).Returns(_userId);
        var userManager = CreateUserManager();
        userManager.Setup(manager => manager.FindByIdAsync(_userId)).ReturnsAsync((ApplicationUser?)null);
        var jwtHandler = CreateJwtHandler(userManager.Object, TimeProvider.System);
        var handler = new RefreshSessionCommandHandler(currentUserService.Object, userManager.Object, jwtHandler, TimeProvider.System);

        await Assert.ThrowsAsync<InvalidCredentialsException>(() => handler.Handle(new RefreshSessionCommand(), CancellationToken.None));
    }

    [Fact]
    public void ValidateLifetime_RejectsLegacyAndOverlongTokens()
    {
        var now = new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc);
        var legacyToken = new JwtSecurityToken(
            claims: [new Claim(ClaimTypes.NameIdentifier, _userId)],
            notBefore: now,
            expires: now.AddHours(1));
        var overlongToken = new JwtSecurityToken(
            claims:
            [
                new Claim(ClaimTypes.NameIdentifier, _userId),
                new Claim(JwtHandler.TokenVersionClaim, JwtHandler.CurrentTokenVersion)
            ],
            notBefore: now,
            expires: now.AddHours(2));

        Assert.False(JwtHandler.ValidateLifetime(now, now.AddHours(1), legacyToken, now));
        Assert.False(JwtHandler.ValidateLifetime(now, now.AddHours(2), overlongToken, now));
    }

    private static JwtHandler CreateJwtHandler(UserManager<ApplicationUser> userManager, TimeProvider timeProvider)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecurityKey"] = _securityKey,
                ["JwtSettings:ValidIssuer"] = _issuer,
                ["JwtSettings:ValidAudience"] = _audience
            })
            .Build();

        return new JwtHandler(configuration, userManager, timeProvider);
    }

    private static TokenValidationParameters CreateValidationParameters(TimeProvider timeProvider)
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _issuer,
            ValidAudience = _audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_securityKey)),
            LifetimeValidator = (notBefore, expires, securityToken, _) => JwtHandler.ValidateLifetime(notBefore, expires, securityToken, timeProvider.GetUtcNow().UtcDateTime),
            ClockSkew = TimeSpan.Zero
        };
    }

    private static Mock<UserManager<ApplicationUser>> CreateUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();

        return new Mock<UserManager<ApplicationUser>>(
            store.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);
    }

    private static ApplicationUser CreateUser()
    {
        return new ApplicationUser
        {
            Id = _userId,
            UserName = "user@example.com",
            Email = "user@example.com",
            Language = LanguageEnum.pl
        };
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;

        public ManualTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }

        public void Advance(TimeSpan duration)
        {
            _utcNow = _utcNow.Add(duration);
        }
    }
}
