using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;
using ProjectX.Application.ApplicationUsers.Commands.LoginApplicationUser;
using ProjectX.Application.Common;
using ProjectX.Application.Common.Exceptions;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Entities;
using ProjectX.Domain.Enums;

namespace ProjectX.UnitTests.Application.ApplicationUsers;

public class LoginApplicationUserCommandHandlerTests
{
    private const string _email = "user@example.com";
    private const string _password = "CorrectPassword1!";

    [Fact]
    public async Task Handle_ReturnsTokenAndResetsFailuresForValidCredentials()
    {
        var user = CreateUser();
        var userManager = CreateUserManager();
        userManager.Setup(manager => manager.FindByEmailAsync(_email)).ReturnsAsync(user);
        userManager.Setup(manager => manager.IsLockedOutAsync(user)).ReturnsAsync(false);
        userManager.Setup(manager => manager.CheckPasswordAsync(user, _password)).ReturnsAsync(true);
        userManager.Setup(manager => manager.ResetAccessFailedCountAsync(user)).ReturnsAsync(IdentityResult.Success);
        userManager.Setup(manager => manager.GetRolesAsync(user)).ReturnsAsync(["Client"]);

        var handler = CreateHandler(userManager.Object);

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.Equal(LanguageEnum.en, result.Language);
        userManager.Verify(manager => manager.ResetAccessFailedCountAsync(user), Times.Once);
        userManager.Verify(manager => manager.AccessFailedAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RecordsFailureAndRejectsInvalidPassword()
    {
        var user = CreateUser();
        var userManager = CreateUserManager();
        userManager.Setup(manager => manager.FindByEmailAsync(_email)).ReturnsAsync(user);
        userManager.Setup(manager => manager.IsLockedOutAsync(user)).ReturnsAsync(false);
        userManager.Setup(manager => manager.CheckPasswordAsync(user, _password)).ReturnsAsync(false);
        userManager.Setup(manager => manager.AccessFailedAsync(user)).ReturnsAsync(IdentityResult.Success);

        var handler = CreateHandler(userManager.Object);

        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            handler.Handle(CreateCommand(), CancellationToken.None));

        userManager.Verify(manager => manager.AccessFailedAsync(user), Times.Once);
        userManager.Verify(manager => manager.ResetAccessFailedCountAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RejectsLockedUserWithoutCheckingPassword()
    {
        var user = CreateUser();
        var userManager = CreateUserManager();
        userManager.Setup(manager => manager.FindByEmailAsync(_email)).ReturnsAsync(user);
        userManager.Setup(manager => manager.IsLockedOutAsync(user)).ReturnsAsync(true);

        var handler = CreateHandler(userManager.Object);

        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            handler.Handle(CreateCommand(), CancellationToken.None));

        userManager.Verify(
            manager => manager.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_RejectsUnknownUserWithoutCheckingPassword()
    {
        var userManager = CreateUserManager();
        userManager.Setup(manager => manager.FindByEmailAsync(_email)).ReturnsAsync((ApplicationUser?)null);

        var handler = CreateHandler(userManager.Object);

        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            handler.Handle(CreateCommand(), CancellationToken.None));

        userManager.Verify(
            manager => manager.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public void LoginPayloads_DoNotExposeSensitiveDataThroughToString()
    {
        var command = CreateCommand();
        var response = new LoginApplicationUserDto
        {
            Token = "sensitive-jwt",
            Language = LanguageEnum.en
        };

        Assert.DoesNotContain(_email, command.ToString());
        Assert.DoesNotContain(_password, command.ToString());
        Assert.DoesNotContain(response.Token, response.ToString());
        Assert.Contains(response.Language.ToString(), response.ToString());
    }

    private static LoginApplicationUserCommandHandler CreateHandler(UserManager<ApplicationUser> userManager)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecurityKey"] = "a-secure-test-key-that-is-at-least-32-characters-long",
                ["JwtSettings:ValidIssuer"] = "ProjectX.UnitTests",
                ["JwtSettings:ValidAudience"] = "ProjectX.UnitTests",
                ["JwtSettings:ExpiryInDays"] = "1"
            })
            .Build();

        return new LoginApplicationUserCommandHandler(
            userManager,
            new JwtHandler(configuration, userManager));
    }

    private static Mock<UserManager<ApplicationUser>> CreateUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();

        return new Mock<UserManager<ApplicationUser>>(
            store.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);
    }

    private static ApplicationUser CreateUser()
    {
        return new ApplicationUser
        {
            Id = "user-id",
            UserName = _email,
            Email = _email,
            Language = LanguageEnum.en
        };
    }

    private static LoginApplicationUserCommand CreateCommand()
    {
        return new LoginApplicationUserCommand
        {
            UserName = _email,
            Password = _password
        };
    }
}
