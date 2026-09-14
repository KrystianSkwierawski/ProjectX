using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ProjectX.API.Infrastructure;
using ProjectX.API.Services;
using ProjectX.Application.Common.Security;

namespace ProjectX.Web.AcceptanceTests.Services;

public sealed class CurrentUserServiceTests
{
    [Theory]
    [InlineData("en", ProjectX.Domain.Enums.LanguageEnum.en)]
    [InlineData("pl", ProjectX.Domain.Enums.LanguageEnum.pl)]
    [InlineData("invalid", ProjectX.Domain.Enums.LanguageEnum.pl)]
    public void Language_UsesSupportedCharacterPreferenceWithAccountFallback(string requested, ProjectX.Domain.Enums.LanguageEnum expected)
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("LanguageEnum", "pl")], "Bearer"))
        };
        context.Request.Headers.AcceptLanguage = requested;
        Assert.Equal(expected, new CurrentUserService(new HttpContextAccessor { HttpContext = context }).Language);
    }

    [Fact]
    public void GetCharacterId_ReturnsCharacterDelegatedByPlayerSession()
    {
        const int characterId = 42;

        var claims = new[] { new Claim(ClaimTypes.Role, ApplicationRoles.Server) };
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"))
        };
        httpContext.Items[PlayerSessionAuthorizationHandler.DelegatedCharacterIdItemKey] = characterId;

        var service = new CurrentUserService(new HttpContextAccessor { HttpContext = httpContext });

        Assert.Equal(characterId, service.GetCharacterId());
    }

    [Fact]
    public void GetAuthenticatedTokenExpirationUtc_ParsesJwtExpirationClaim()
    {
        var expectedExpiration = new DateTimeOffset(2026, 8, 10, 13, 0, 0, TimeSpan.Zero);
        var claims = new[] { new Claim(JwtRegisteredClaimNames.Exp, expectedExpiration.ToUnixTimeSeconds().ToString()) };
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims))
        };

        var service = new CurrentUserService(new HttpContextAccessor { HttpContext = httpContext });

        var expiration = service.GetAuthenticatedTokenExpirationUtc();

        Assert.Equal(expectedExpiration, expiration);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-number")]
    public void GetAuthenticatedTokenExpirationUtc_ReturnsNullForInvalidClaim(string? claimValue)
    {
        var claims = claimValue is null
            ? Array.Empty<Claim>()
            : [new Claim(JwtRegisteredClaimNames.Exp, claimValue)];

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims))
        };

        var service = new CurrentUserService(new HttpContextAccessor { HttpContext = httpContext });

        Assert.Null(service.GetAuthenticatedTokenExpirationUtc());
    }

    [Fact]
    public void GetAuthenticatedSessionStartedAtUtc_ParsesSessionClaim()
    {
        var expectedSessionStart = new DateTimeOffset(2026, 8, 10, 12, 0, 0, TimeSpan.Zero);
        var claims = new[]
        {
            new Claim(SessionTokenPolicy.SessionStartedAtClaim, expectedSessionStart.ToUnixTimeSeconds().ToString())
        };
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims))
        };

        var service = new CurrentUserService(new HttpContextAccessor { HttpContext = httpContext });

        Assert.Equal(expectedSessionStart, service.GetAuthenticatedSessionStartedAtUtc());
    }
}
