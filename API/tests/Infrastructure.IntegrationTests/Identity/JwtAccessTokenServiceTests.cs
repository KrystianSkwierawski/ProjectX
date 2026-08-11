using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
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
        var originalToken = service.Create(user);

        timeProvider.Advance(TimeSpan.FromMinutes(55));
        var renewedToken = service.Create(user);
        var nextRenewedToken = service.Create(user);

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
                new Claim(SessionTokenPolicy.VersionClaim, SessionTokenPolicy.CurrentVersion)
            ],
            notBefore: now,
            expires: now.AddHours(2));

        Assert.False(JwtAccessTokenService.ValidateLifetime(now, now.AddHours(1), legacyToken, now));
        Assert.False(JwtAccessTokenService.ValidateLifetime(now, now.AddHours(2), overlongToken, now));
    }

    private static JwtAccessTokenService CreateService(TimeProvider timeProvider)
    {
        return new JwtAccessTokenService(new JwtOptions(SecurityKey, Issuer, Audience), timeProvider);
    }

    private static TokenValidationParameters CreateValidationParameters(TimeProvider timeProvider)
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = Issuer,
            ValidAudience = Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecurityKey)),
            LifetimeValidator = (notBefore, expires, securityToken, _) => JwtAccessTokenService.ValidateLifetime(notBefore, expires, securityToken, timeProvider.GetUtcNow().UtcDateTime),
            ClockSkew = TimeSpan.Zero
        };
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
