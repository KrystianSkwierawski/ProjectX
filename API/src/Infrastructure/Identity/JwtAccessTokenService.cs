using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Application.Common.Models;
using ProjectX.Application.Common.Security;
using ProjectX.Domain.Enums;
using JsonWebToken = Microsoft.IdentityModel.JsonWebTokens.JsonWebToken;

namespace ProjectX.Infrastructure.Identity;

public sealed class JwtAccessTokenService : IAccessTokenService
{
    private readonly JwtOptions _options;
    private readonly TimeProvider _timeProvider;

    public JwtAccessTokenService(JwtOptions options, TimeProvider timeProvider)
    {
        _options = options;
        _timeProvider = timeProvider;
    }

    public string Create(AuthenticatedApplicationUser user, DateTimeOffset? sessionStartedAtUtc = null)
    {
        var issuedAtUtc = _timeProvider.GetUtcNow().ToUniversalTime();
        var sessionStartedAt = sessionStartedAtUtc?.ToUniversalTime() ?? issuedAtUtc;

        if (sessionStartedAt > issuedAtUtc)
        {
            throw new ArgumentOutOfRangeException(nameof(sessionStartedAtUtc), "The session start cannot be in the future.");
        }

        var sessionExpiresAt = sessionStartedAt.Add(SessionTokenPolicy.MaximumSessionLifetime);

        if (sessionExpiresAt <= issuedAtUtc)
        {
            throw new InvalidOperationException("The authenticated session has reached its maximum lifetime.");
        }

        var tokenExpiresAt = issuedAtUtc.Add(SessionTokenPolicy.Lifetime);

        if (tokenExpiresAt > sessionExpiresAt)
        {
            tokenExpiresAt = sessionExpiresAt;
        }

        var issuedAt = issuedAtUtc.UtcDateTime;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.UserName),
            new(nameof(LanguageEnum), user.Language.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, EpochTime.GetIntDate(issuedAt).ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64),
            new(
                SessionTokenPolicy.SessionStartedAtClaim,
                sessionStartedAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
                ClaimValueTypes.Integer64),
            new(SessionTokenPolicy.VersionClaim, SessionTokenPolicy.CurrentVersion)
        };

        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var token = new JwtSecurityToken(
            issuer: _options.ValidIssuer,
            audience: _options.ValidAudience,
            claims: claims,
            notBefore: issuedAt,
            expires: tokenExpiresAt.UtcDateTime,
            signingCredentials: CreateSigningCredentials());

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static bool ValidateLifetime(DateTime? notBefore, DateTime? expires, SecurityToken securityToken, DateTime utcNow)
    {
        var claims = securityToken switch
        {
            JwtSecurityToken jwt => jwt.Claims,
            JsonWebToken jwt => jwt.Claims,
            _ => null
        };

        var tokenVersion = claims?.FirstOrDefault(claim => claim.Type == SessionTokenPolicy.VersionClaim)?.Value;
        var sessionStartedAtClaim = claims?.FirstOrDefault(claim => claim.Type == SessionTokenPolicy.SessionStartedAtClaim)?.Value;

        if (notBefore is null
            || expires is null
            || tokenVersion != SessionTokenPolicy.CurrentVersion
            || !long.TryParse(sessionStartedAtClaim, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sessionStartedAtSeconds))
        {
            return false;
        }

        DateTime sessionStartedAt;

        try
        {
            sessionStartedAt = DateTimeOffset.FromUnixTimeSeconds(sessionStartedAtSeconds).UtcDateTime;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }

        DateTime sessionExpiresAt;

        try
        {
            sessionExpiresAt = sessionStartedAt.Add(SessionTokenPolicy.MaximumSessionLifetime);
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }

        return sessionStartedAt <= utcNow
            && sessionStartedAt <= notBefore.Value
            && notBefore.Value <= utcNow
            && expires.Value > utcNow
            && expires.Value - notBefore.Value <= SessionTokenPolicy.Lifetime
            && expires.Value <= sessionExpiresAt;
    }

    public static TokenValidationParameters CreateValidationParameters(JwtOptions options, TimeProvider timeProvider)
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = options.ValidIssuer,
            ValidAudience = options.ValidAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SecurityKey)),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            LifetimeValidator = (notBefore, expires, securityToken, _) =>
                ValidateLifetime(notBefore, expires, securityToken, timeProvider.GetUtcNow().UtcDateTime),
            ClockSkew = TimeSpan.Zero
        };
    }

    private SigningCredentials CreateSigningCredentials()
    {
        var secret = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecurityKey));

        return new SigningCredentials(secret, SecurityAlgorithms.HmacSha256);
    }
}
