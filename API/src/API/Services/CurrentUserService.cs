using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ProjectX.API.Infrastructure;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Enums;

namespace ProjectX.API.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetId()
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext?.User?.IsInRole("Server") == true
            && httpContext.Items.TryGetValue(PlayerSessionAuthorizationHandler.DelegatedUserIdItemKey, out var delegatedUserId)
            && delegatedUserId is string userId)
        {
            return userId;
        }

        return GetAuthenticatedUserId();
    }

    public string GetAuthenticatedUserId()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    }

    public DateTimeOffset? GetAuthenticatedTokenExpirationUtc()
    {
        var expirationClaim = _httpContextAccessor.HttpContext?.User?.FindFirstValue(JwtRegisteredClaimNames.Exp);

        return long.TryParse(expirationClaim, NumberStyles.Integer, CultureInfo.InvariantCulture, out var expirationSeconds)
            ? DateTimeOffset.FromUnixTimeSeconds(expirationSeconds)
            : null;
    }

    public LanguageEnum Language => _httpContextAccessor.HttpContext?.User == null
        ? LanguageEnum.en
        : (LanguageEnum)Enum.Parse(typeof(LanguageEnum), _httpContextAccessor.HttpContext.User.FindFirstValue(nameof(LanguageEnum)) ?? LanguageEnum.en.ToString());

    public List<string>? Roles => _httpContextAccessor.HttpContext?.User?.FindAll(ClaimTypes.Role).Select(x => x.Value).ToList();
}
