namespace ProjectX.Application.ApplicationUsers.Commands.LoginApplicationUser;
public class LoginApplicationUserDto
{
    public string Token { get; set; }

    public override string ToString()
    {
        return $"{nameof(LoginApplicationUserDto)} {{ Token = {Token} }}";
    }
}
