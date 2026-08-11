using MediatR;
using Microsoft.AspNetCore.Identity;
using ProjectX.Application.Common;
using ProjectX.Application.Common.Exceptions;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Entities;

namespace ProjectX.Application.ApplicationUsers.Commands.RefreshSession;

public record RefreshSessionCommand : IRequest<RefreshSessionDto>
{
    public override string ToString()
    {
        return nameof(RefreshSessionCommand);
    }
}

public class RefreshSessionCommandHandler : IRequestHandler<RefreshSessionCommand, RefreshSessionDto>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtHandler _jwtHandler;
    private readonly TimeProvider _timeProvider;

    public RefreshSessionCommandHandler(ICurrentUserService currentUserService, UserManager<ApplicationUser> userManager, JwtHandler jwtHandler, TimeProvider timeProvider)
    {
        _currentUserService = currentUserService;
        _userManager = userManager;
        _jwtHandler = jwtHandler;
        _timeProvider = timeProvider;
    }

    public async Task<RefreshSessionDto> Handle(RefreshSessionCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.FindByIdAsync(_currentUserService.GetAuthenticatedUserId());

        if (user is null || await _userManager.IsLockedOutAsync(user))
        {
            throw new InvalidCredentialsException();
        }

        var tokenExpiresAtUtc = _currentUserService.GetAuthenticatedTokenExpirationUtc();
        var utcNow = _timeProvider.GetUtcNow();

        if (tokenExpiresAtUtc is null || tokenExpiresAtUtc <= utcNow)
        {
            throw new InvalidCredentialsException();
        }

        if (tokenExpiresAtUtc > utcNow.Add(JwtHandler.RefreshWindow))
        {
            throw new ForbiddenAccessException();
        }

        return new RefreshSessionDto
        {
            Token = await _jwtHandler.GenerateToken(user),
            Language = user.Language
        };
    }
}
