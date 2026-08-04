using MediatR;
using Microsoft.AspNetCore.Identity;
using ProjectX.Application.Common;
using ProjectX.Application.Common.Exceptions;
using ProjectX.Domain.Entities;

namespace ProjectX.Application.ApplicationUsers.Commands.LoginApplicationUser;
public record LoginApplicationUserCommand : IRequest<LoginApplicationUserDto>
{
    public required string UserName { get; set; }

    public required string Password { get; set; }
}

public class LoginApplicationUserCommandHandler : IRequestHandler<LoginApplicationUserCommand, LoginApplicationUserDto>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtHandler _jwtHandler;

    public LoginApplicationUserCommandHandler(UserManager<ApplicationUser> userManager, JwtHandler jwtHandler)
    {
        _userManager = userManager;
        _jwtHandler = jwtHandler;
    }

    public async Task<LoginApplicationUserDto> Handle(LoginApplicationUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.UserName);

        if (user?.Email is null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new InvalidCredentialsException();
        }

        var token = await _jwtHandler.GenerateToken(user);

        return new LoginApplicationUserDto
        {
            Token = token,
            Language = user.Language
        };
    }
}
