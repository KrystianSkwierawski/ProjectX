using MediatR;
using ProjectX.Application.Common.Exceptions;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Application.Common.Security;

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
    private readonly IApplicationUserAuthenticationService _authenticationService;
    private readonly IAccessTokenService _accessTokenService;
    private readonly TimeProvider _timeProvider;

    public RefreshSessionCommandHandler(
        ICurrentUserService currentUserService,
        IApplicationUserAuthenticationService authenticationService,
        IAccessTokenService accessTokenService,
        TimeProvider timeProvider)
    {
        _currentUserService = currentUserService;
        _authenticationService = authenticationService;
        _accessTokenService = accessTokenService;
        _timeProvider = timeProvider;
    }

    public async Task<RefreshSessionDto> Handle(RefreshSessionCommand request, CancellationToken cancellationToken)
    {
        var user = await _authenticationService.FindActiveByIdAsync(_currentUserService.GetAuthenticatedUserId(), cancellationToken)
            ?? throw new InvalidCredentialsException();

        var tokenExpiresAtUtc = _currentUserService.GetAuthenticatedTokenExpirationUtc();
        var utcNow = _timeProvider.GetUtcNow();

        if (tokenExpiresAtUtc is null || tokenExpiresAtUtc <= utcNow)
        {
            throw new InvalidCredentialsException();
        }

        if (tokenExpiresAtUtc > utcNow.Add(SessionTokenPolicy.RefreshWindow))
        {
            throw new ForbiddenAccessException();
        }

        return new RefreshSessionDto
        {
            Token = _accessTokenService.Create(user),
            Language = user.Language
        };
    }
}
