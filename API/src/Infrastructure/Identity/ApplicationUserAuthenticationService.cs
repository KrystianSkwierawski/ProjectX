using Microsoft.AspNetCore.Identity;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Application.Common.Models;

namespace ProjectX.Infrastructure.Identity;

public sealed class ApplicationUserAuthenticationService : IApplicationUserAuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ApplicationUserAuthenticationService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<AuthenticatedApplicationUser?> AuthenticateAsync(string email, string password, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.FindByEmailAsync(email);

        if (user?.Email is null || await _userManager.IsLockedOutAsync(user))
        {
            return null;
        }

        if (!await _userManager.CheckPasswordAsync(user, password))
        {
            await _userManager.AccessFailedAsync(user);

            return null;
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        return await CreateAuthenticatedUserAsync(user, cancellationToken);
    }

    public async Task<AuthenticatedApplicationUser?> FindActiveByIdAsync(string userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.FindByIdAsync(userId);

        if (user?.Email is null || await _userManager.IsLockedOutAsync(user))
        {
            return null;
        }

        return await CreateAuthenticatedUserAsync(user, cancellationToken);
    }

    private async Task<AuthenticatedApplicationUser> CreateAuthenticatedUserAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var roles = await _userManager.GetRolesAsync(user);

        cancellationToken.ThrowIfCancellationRequested();

        return new AuthenticatedApplicationUser(
            user.Id,
            user.Email!,
            user.UserName ?? user.Email!,
            user.Language,
            roles.ToArray());
    }
}
