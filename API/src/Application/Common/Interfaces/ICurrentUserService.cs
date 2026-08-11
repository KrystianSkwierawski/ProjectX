using ProjectX.Domain.Enums;

namespace ProjectX.Application.Common.Interfaces;
public interface ICurrentUserService
{
    string GetId();

    string GetAuthenticatedUserId();

    DateTimeOffset? GetAuthenticatedTokenExpirationUtc();

    LanguageEnum Language { get; }

    List<string>? Roles { get; }
}
