using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ProjectX.Application.Common;
public class JwtHandler
{
    private readonly IConfiguration _configuration;
    private readonly IConfigurationSection _jwtSettings;
    public JwtHandler(IConfiguration configuration)
    {
        _configuration = configuration;
        _jwtSettings = _configuration.GetSection("JwtSettings");
    }

    private SigningCredentials GetSigningCredentials()
    {
        var key = Encoding.UTF8.GetBytes(_jwtSettings.GetSection("SecurityKey").Value);
        var secret = new SymmetricSecurityKey(key);

        return new SigningCredentials(secret, SecurityAlgorithms.HmacSha256);
    }

    public List<Claim> GetClamis(string email, string userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.NameIdentifier, userId)
        };

        return claims;
    }

    public string GenerateToken(List<Claim> claims)
    {
        JwtSecurityToken tokenOptions = new(
            issuer: _jwtSettings.GetSection("ValidIssuer").Value,
            audience: _jwtSettings.GetSection("ValidAudience").Value,
            claims: claims,
            expires: DateTime.Now.AddDays(Convert.ToDouble(_jwtSettings.GetSection("ExpiryInDays").Value)),
            signingCredentials: GetSigningCredentials()
        );

        string token = new JwtSecurityTokenHandler().WriteToken(tokenOptions);
        return token;
    }
}
