using ProjectX.Domain.Enums;

namespace ProjectX.Application.Common.Models;

public sealed record AuthenticatedApplicationUser(
    string Id,
    string Email,
    string UserName,
    LanguageEnum Language,
    IReadOnlyCollection<string> Roles);
