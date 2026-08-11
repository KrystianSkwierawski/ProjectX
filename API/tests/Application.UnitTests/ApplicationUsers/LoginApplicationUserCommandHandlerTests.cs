using Moq;
using ProjectX.Application.ApplicationUsers.Commands.LoginApplicationUser;
using ProjectX.Application.Common.Exceptions;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Application.Common.Models;
using ProjectX.Application.Common.Security;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.UnitTests.ApplicationUsers;

public class LoginApplicationUserCommandHandlerTests
{
    private const string Email = "user@example.com";
    private const string Password = "CorrectPassword1!";

    [Fact]
    public async Task Handle_ReturnsTokenAndLanguageForValidCredentials()
    {
        var user = CreateUser();
        var authentication = new Mock<IApplicationUserAuthenticationService>();
        authentication
            .Setup(service => service.AuthenticateAsync(Email, Password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        var accessTokens = new Mock<IAccessTokenService>();
        accessTokens.Setup(service => service.Create(user)).Returns("signed-jwt");
        var handler = new LoginApplicationUserCommandHandler(authentication.Object, accessTokens.Object);

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal("signed-jwt", result.Token);
        Assert.Equal(LanguageEnum.en, result.Language);
        accessTokens.Verify(service => service.Create(user), Times.Once);
    }

    [Fact]
    public async Task Handle_RejectsInvalidCredentialsWithoutIssuingToken()
    {
        var authentication = new Mock<IApplicationUserAuthenticationService>();
        authentication
            .Setup(service => service.AuthenticateAsync(Email, Password, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthenticatedApplicationUser?)null);
        var accessTokens = new Mock<IAccessTokenService>();
        var handler = new LoginApplicationUserCommandHandler(authentication.Object, accessTokens.Object);

        await Assert.ThrowsAsync<InvalidCredentialsException>(() => handler.Handle(CreateCommand(), CancellationToken.None));

        accessTokens.Verify(service => service.Create(It.IsAny<AuthenticatedApplicationUser>()), Times.Never);
    }

    [Fact]
    public void LoginPayloads_DoNotExposeSensitiveDataThroughToString()
    {
        var command = CreateCommand();
        var response = new LoginApplicationUserDto { Token = "sensitive-jwt", Language = LanguageEnum.en };

        Assert.DoesNotContain(Email, command.ToString());
        Assert.DoesNotContain(Password, command.ToString());
        Assert.DoesNotContain(response.Token, response.ToString());
        Assert.Contains(response.Language.ToString(), response.ToString());
    }

    private static AuthenticatedApplicationUser CreateUser()
    {
        return new AuthenticatedApplicationUser("user-id", Email, Email, LanguageEnum.en, [ApplicationRoles.Client]);
    }

    private static LoginApplicationUserCommand CreateCommand()
    {
        return new LoginApplicationUserCommand { UserName = Email, Password = Password };
    }
}
