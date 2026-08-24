using ProjectX.Application.Common.Models;

namespace ProjectX.Application.Common.Interfaces;

public interface IAccessTokenService
{
    string Create(AuthenticatedApplicationUser user, DateTimeOffset? sessionStartedAtUtc = null);
}
