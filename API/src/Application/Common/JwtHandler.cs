using System.IdentityModel.Tokens.Jwt;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ProjectX.Domain.Entities;
using ProjectX.Domain.Enums;
using JsonWebToken = Microsoft.IdentityModel.JsonWebTokens.JsonWebToken;

namespace ProjectX.Application.Common;

public class JwtHandler
{
    public const string TokenVersionClaim = "token_version";
    public const string CurrentTokenVersion = "1";

    public static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(1);
    public static readonly TimeSpan RefreshWindow = TimeSpan.FromMinutes(5);

    private readonly IConfigurationSection _jwtSettings;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly TimeProvider _timeProvider;

    public JwtHandler(IConfiguration configuration, UserManager<ApplicationUser> userManager, TimeProvider timeProvider)
    {
        _jwtSettings = configuration.GetSection("JwtSettings");
        _userManager = userManager;
        _timeProvider = timeProvider;
    }

    private SigningCredentials GetSigningCredentials()
    {
        var secret = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetRequiredSetting("SecurityKey")));

        return new SigningCredentials(secret, SecurityAlgorithms.HmacSha256);
    }

    private string GetRequiredSetting(string name)
    {
        var value = _jwtSettings[name];

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"JwtSettings:{name} is required.");
        }

        return value;
    }

    public async Task<string> GenerateToken(ApplicationUser user)
    {
        var issuedAt = _timeProvider.GetUtcNow().UtcDateTime;

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
            new Claim(nameof(LanguageEnum), user.Language.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, EpochTime.GetIntDate(issuedAt).ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64),
            new Claim(TokenVersionClaim, CurrentTokenVersion)
        };

        var roles = await _userManager.GetRolesAsync(user);

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        JwtSecurityToken tokenOptions = new(
            issuer: GetRequiredSetting("ValidIssuer"),
            audience: GetRequiredSetting("ValidAudience"),
            claims: claims,
            notBefore: issuedAt,
            expires: issuedAt.Add(TokenLifetime),
            signingCredentials: GetSigningCredentials()
        );

        return new JwtSecurityTokenHandler().WriteToken(tokenOptions);
    }

    public static bool ValidateLifetime(DateTime? notBefore, DateTime? expires, SecurityToken securityToken, DateTime utcNow)
    {
        var tokenVersion = securityToken switch
        {
            JwtSecurityToken jwt => jwt.Claims.FirstOrDefault(claim => claim.Type == TokenVersionClaim)?.Value,
            JsonWebToken jwt => jwt.Claims.FirstOrDefault(claim => claim.Type == TokenVersionClaim)?.Value,
            _ => null
        };

        if (notBefore is null || expires is null || tokenVersion != CurrentTokenVersion)
        {
            return false;
        }

        return notBefore.Value <= utcNow && expires.Value > utcNow && expires.Value - notBefore.Value <= TokenLifetime;
    }
}
