using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using ProjectX.Application.Common.Models;
using ProjectX.Application.Common.Security;
using ProjectX.Domain.Enums;
using ProjectX.Infrastructure.Identity;
using JsonWebTokenHandler = Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler;

namespace ProjectX.Infrastructure.IntegrationTests.Identity;

public class JwtAccessTokenServiceTests
{
    private const string UserId = "user-id";
    private const string SecurityKey = "a-secure-test-key-that-is-at-least-32-characters-long";
    private const string Issuer = "ProjectX.Infrastructure.IntegrationTests";
    private const string Audience = "ProjectX.Infrastructure.IntegrationTests";

    [Fact]
    public async Task Create_IssuesUniqueTokensAndKeepsTheRenewalHandoverWindowValid()
    {
        var timeProvider = new ManualTimeProvider(new DateTimeOffset(2026, 8, 10, 12, 0, 0, TimeSpan.Zero));
        var service = CreateService(timeProvider);
        var user = CreateUser();
        var sessionStartedAt = timeProvider.GetUtcNow();
        var originalToken = service.Create(user);

        timeProvider.Advance(TimeSpan.FromMinutes(55));
        var renewedToken = service.Create(user, sessionStartedAt);
        var nextRenewedToken = service.Create(user, sessionStartedAt);

        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = CreateValidationParameters(timeProvider);
        var token = tokenHandler.ReadJwtToken(renewedToken);

        Assert.Equal(UserId, token.Claims.Single(claim => claim.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Contains(token.Claims, claim => claim.Type == ClaimTypes.Role && claim.Value == ApplicationRoles.Server);
        Assert.NotEqual(renewedToken, nextRenewedToken);

        tokenHandler.ValidateToken(originalToken, validationParameters, out _);
        tokenHandler.ValidateToken(renewedToken, validationParameters, out _);
        var middlewareValidation = await new JsonWebTokenHandler().ValidateTokenAsync(renewedToken, validationParameters);
        Assert.True(middlewareValidation.IsValid, middlewareValidation.Exception?.ToString());

        timeProvider.Advance(TimeSpan.FromMinutes(4) + TimeSpan.FromSeconds(59));
        tokenHandler.ValidateToken(originalToken, validationParameters, out _);
        timeProvider.Advance(TimeSpan.FromSeconds(2));
        Assert.Throws<SecurityTokenInvalidLifetimeException>(() => tokenHandler.ValidateToken(originalToken, validationParameters, out _));
        tokenHandler.ValidateToken(renewedToken, validationParameters, out _);
    }

    [Fact]
    public void ValidateLifetime_RejectsLegacyAndOverlongTokens()
    {
        var now = new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc);
        var legacyToken = new JwtSecurityToken(
            claims: [new Claim(ClaimTypes.NameIdentifier, UserId)],
            notBefore: now,
            expires: now.AddHours(1));
        var overlongToken = new JwtSecurityToken(
            claims:
            [
                new Claim(ClaimTypes.NameIdentifier, UserId),
                new Claim(
                    SessionTokenPolicy.SessionStartedAtClaim,
                    new DateTimeOffset(now).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)),
                new Claim(SessionTokenPolicy.VersionClaim, SessionTokenPolicy.CurrentVersion)
            ],
            notBefore: now,
            expires: now.AddHours(2));

        Assert.False(JwtAccessTokenService.ValidateLifetime(now, now.AddHours(1), legacyToken, now));
        Assert.False(JwtAccessTokenService.ValidateLifetime(now, now.AddHours(2), overlongToken, now));
    }

    [Fact]
    public void Create_PreservesSessionStartAndCapsTokenAtMaximumSessionLifetime()
    {
        var sessionStartedAt = new DateTimeOffset(2026, 8, 10, 12, 0, 0, TimeSpan.Zero);
        var timeProvider = new ManualTimeProvider(sessionStartedAt.Add(SessionTokenPolicy.MaximumSessionLifetime).AddMinutes(-4));
        var token = new JwtSecurityTokenHandler().ReadJwtToken(CreateService(timeProvider).Create(CreateUser(), sessionStartedAt));

        Assert.Equal(
            sessionStartedAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
            token.Claims.Single(claim => claim.Type == SessionTokenPolicy.SessionStartedAtClaim).Value);
        Assert.Equal(sessionStartedAt.Add(SessionTokenPolicy.MaximumSessionLifetime).UtcDateTime, token.ValidTo);
    }

    [Fact]
    public void JwtOptions_RejectsSecurityKeysShorterThan256Bits()
    {
        var exception = Assert.Throws<ArgumentException>(() => new JwtOptions("too-short", Issuer, Audience));

        Assert.Contains("at least 32 UTF-8 bytes", exception.Message, StringComparison.Ordinal);
    }

    private static JwtAccessTokenService CreateService(TimeProvider timeProvider)
    {
        return new JwtAccessTokenService(new JwtOptions(SecurityKey, Issuer, Audience), timeProvider);
    }

    private static TokenValidationParameters CreateValidationParameters(TimeProvider timeProvider)
    {
        return JwtAccessTokenService.CreateValidationParameters(
            new JwtOptions(SecurityKey, Issuer, Audience),
            timeProvider);
    }

    private static AuthenticatedApplicationUser CreateUser()
    {
        return new AuthenticatedApplicationUser(
            UserId,
            "user@example.com",
            "user@example.com",
            LanguageEnum.pl,
            [ApplicationRoles.Server]);
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;

        public ManualTimeProvider(DateTimeOffset utcNow) => _utcNow = utcNow;
        public override DateTimeOffset GetUtcNow() => _utcNow;
        public void Advance(TimeSpan duration) => _utcNow = _utcNow.Add(duration);
    }
}
