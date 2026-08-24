using MediatR;
using ProjectX.Application.Common.Exceptions;
using ProjectX.Application.Common.Interfaces;

namespace ProjectX.Application.ApplicationUsers.Commands.LoginApplicationUser;

public record LoginApplicationUserCommand : IRequest<LoginApplicationUserDto>
{
    public required string UserName { get; set; }

    public required string Password { get; set; }

    public override string ToString()
    {
        return nameof(LoginApplicationUserCommand);
    }
}

public class LoginApplicationUserCommandHandler : IRequestHandler<LoginApplicationUserCommand, LoginApplicationUserDto>
{
    private readonly IApplicationUserAuthenticationService _authenticationService;
    private readonly IAccessTokenService _accessTokenService;

    public LoginApplicationUserCommandHandler(IApplicationUserAuthenticationService authenticationService, IAccessTokenService accessTokenService)
    {
        _authenticationService = authenticationService;
        _accessTokenService = accessTokenService;
    }

    public async Task<LoginApplicationUserDto> Handle(LoginApplicationUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _authenticationService.AuthenticateAsync(request.UserName, request.Password, cancellationToken)
            ?? throw new InvalidCredentialsException();

        return new LoginApplicationUserDto
        {
            Token = _accessTokenService.Create(user),
            Language = user.Language
        };
    }
}
