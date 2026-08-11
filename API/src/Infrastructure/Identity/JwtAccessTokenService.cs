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

    public string Create(AuthenticatedApplicationUser user)
    {
        var issuedAt = _timeProvider.GetUtcNow().UtcDateTime;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.UserName),
            new(nameof(LanguageEnum), user.Language.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, EpochTime.GetIntDate(issuedAt).ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64),
            new(SessionTokenPolicy.VersionClaim, SessionTokenPolicy.CurrentVersion)
        };

        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var token = new JwtSecurityToken(
            issuer: _options.ValidIssuer,
            audience: _options.ValidAudience,
            claims: claims,
            notBefore: issuedAt,
            expires: issuedAt.Add(SessionTokenPolicy.Lifetime),
            signingCredentials: CreateSigningCredentials());

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static bool ValidateLifetime(DateTime? notBefore, DateTime? expires, SecurityToken securityToken, DateTime utcNow)
    {
        var tokenVersion = securityToken switch
        {
            JwtSecurityToken jwt => jwt.Claims.FirstOrDefault(claim => claim.Type == SessionTokenPolicy.VersionClaim)?.Value,
            JsonWebToken jwt => jwt.Claims.FirstOrDefault(claim => claim.Type == SessionTokenPolicy.VersionClaim)?.Value,
            _ => null
        };

        if (notBefore is null || expires is null || tokenVersion != SessionTokenPolicy.CurrentVersion)
        {
            return false;
        }

        return notBefore.Value <= utcNow
            && expires.Value > utcNow
            && expires.Value - notBefore.Value <= SessionTokenPolicy.Lifetime;
    }

    private SigningCredentials CreateSigningCredentials()
    {
        var secret = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecurityKey));
        return new SigningCredentials(secret, SecurityAlgorithms.HmacSha256);
    }
}
