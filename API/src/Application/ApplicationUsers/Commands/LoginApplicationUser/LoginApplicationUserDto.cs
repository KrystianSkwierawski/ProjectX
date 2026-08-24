using ProjectX.Domain.Enums;

namespace ProjectX.Application.ApplicationUsers.Commands.LoginApplicationUser;

public class LoginApplicationUserDto
{
    public required string Token { get; set; }

    public LanguageEnum Language { get; set; }

    public override string ToString()
    {
        return $"{nameof(LoginApplicationUserDto)} {{ Language = {Language} }}";
    }
}
