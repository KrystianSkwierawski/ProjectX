using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ProjectX.Domain.Entities;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.Common;

public class JwtHandler
{
    private readonly IConfiguration _configuration;
    private readonly IConfigurationSection _jwtSettings;
    private readonly UserManager<ApplicationUser> _userManager;

    public JwtHandler(IConfiguration configuration, UserManager<ApplicationUser> userManager)
    {
        _configuration = configuration;
        _jwtSettings = _configuration.GetSection("JwtSettings");
        _userManager = userManager;
    }

    private SigningCredentials GetSigningCredentials()
    {
        var key = Encoding.UTF8.GetBytes(_jwtSettings.GetSection("SecurityKey").Value);
        var secret = new SymmetricSecurityKey(key);

        return new SigningCredentials(secret, SecurityAlgorithms.HmacSha256);
    }

    public async Task<string> GenerateToken(ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
            new Claim(nameof(LanguageEnum), user.Language.ToString())
        };

        var roles = await _userManager.GetRolesAsync(user);

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        JwtSecurityToken tokenOptions = new(
            issuer: _jwtSettings.GetSection("ValidIssuer").Value,
            audience: _jwtSettings.GetSection("ValidAudience").Value,
            claims: claims,
            expires: DateTime.Now.AddDays(Convert.ToDouble(_jwtSettings.GetSection("ExpiryInDays").Value)),
            signingCredentials: GetSigningCredentials()
        );

        return new JwtSecurityTokenHandler().WriteToken(tokenOptions);
    }
}
