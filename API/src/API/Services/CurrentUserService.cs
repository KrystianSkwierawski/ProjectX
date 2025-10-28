using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using ProjectX.Application.Common.Interfaces;

namespace ProjectX.API.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly TokenValidationParameters _tokenValidationParameters;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, TokenValidationParameters tokenValidationParameters)
    {
        _httpContextAccessor = httpContextAccessor;
        _tokenValidationParameters = tokenValidationParameters;
    }

    public string GetId()
    {
        if ((_httpContextAccessor.HttpContext?.User?.IsInRole("Server") ?? false)
            && _httpContextAccessor.HttpContext?.Request.Headers.TryGetValue("ClientToken", out var token) == true)
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, _tokenValidationParameters, out var validatedToken);

            var userId = principal.Claims
                .Where(x => x.Type == ClaimTypes.NameIdentifier)
                .Select(x => x.Value)
                .First();

            return userId;
        }

        return _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    }

    public List<string>? Roles => _httpContextAccessor.HttpContext?.User?.FindAll(ClaimTypes.Role).Select(x => x.Value).ToList();
}
