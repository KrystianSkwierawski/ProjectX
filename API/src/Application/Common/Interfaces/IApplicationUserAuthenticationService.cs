using ProjectX.Application.Common.Models;

namespace ProjectX.Application.Common.Interfaces;

public interface IApplicationUserAuthenticationService
{
    Task<AuthenticatedApplicationUser?> AuthenticateAsync(string email, string password, CancellationToken cancellationToken);

    Task<AuthenticatedApplicationUser?> FindActiveByIdAsync(string userId, CancellationToken cancellationToken);
}
