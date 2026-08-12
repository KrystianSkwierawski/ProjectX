using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ProjectX.API.Services;
using ProjectX.Application.Common.Security;

namespace ProjectX.Web.AcceptanceTests.Services;

public sealed class CurrentUserServiceTests
{
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
