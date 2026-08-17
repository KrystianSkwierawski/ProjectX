using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ProjectX.API.Infrastructure;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Application.Common.Security;
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

        if (httpContext?.User?.IsInRole(ApplicationRoles.Server) == true
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

    public int? GetCharacterId()
    {
        var httpContext = _httpContextAccessor.HttpContext;

        return httpContext?.User?.IsInRole(ApplicationRoles.Server) == true
            && httpContext.Items.TryGetValue(PlayerSessionAuthorizationHandler.DelegatedCharacterIdItemKey, out var delegatedCharacterId)
            && delegatedCharacterId is int characterId
                ? characterId
                : null;
    }

    public DateTimeOffset? GetAuthenticatedSessionStartedAtUtc()
    {
        return GetUnixTimeClaim(SessionTokenPolicy.SessionStartedAtClaim);
    }

    public DateTimeOffset? GetAuthenticatedTokenExpirationUtc()
    {
        return GetUnixTimeClaim(JwtRegisteredClaimNames.Exp);
    }

    private DateTimeOffset? GetUnixTimeClaim(string claimType)
    {
        var claim = _httpContextAccessor.HttpContext?.User?.FindFirstValue(claimType);

        return long.TryParse(claim, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds)
            && seconds >= DateTimeOffset.MinValue.ToUnixTimeSeconds()
            && seconds <= DateTimeOffset.MaxValue.ToUnixTimeSeconds()
            ? DateTimeOffset.FromUnixTimeSeconds(seconds)
            : null;
    }

    public LanguageEnum Language => _httpContextAccessor.HttpContext?.User == null
        ? LanguageEnum.en
        : (LanguageEnum)Enum.Parse(typeof(LanguageEnum), _httpContextAccessor.HttpContext.User.FindFirstValue(nameof(LanguageEnum)) ?? LanguageEnum.en.ToString());

    public List<string>? Roles => _httpContextAccessor.HttpContext?.User?.FindAll(ClaimTypes.Role).Select(x => x.Value).ToList();
}
