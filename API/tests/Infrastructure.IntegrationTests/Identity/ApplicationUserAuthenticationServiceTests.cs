using Microsoft.AspNetCore.Identity;
using Moq;
using ProjectX.Application.Common.Security;
using ProjectX.Domain.Enums;
using ProjectX.Infrastructure.Identity;

namespace ProjectX.Infrastructure.IntegrationTests.Identity;

public class ApplicationUserAuthenticationServiceTests
{
    private const string Email = "user@example.com";
    private const string Password = "CorrectPassword1!";

    [Fact]
    public async Task AuthenticateAsync_ReturnsAuthenticatedUserAndResetsFailuresForValidCredentials()
    {
        var user = CreateUser();
        var userManager = CreateUserManager();
        userManager.Setup(manager => manager.FindByEmailAsync(Email)).ReturnsAsync(user);
        userManager.Setup(manager => manager.IsLockedOutAsync(user)).ReturnsAsync(false);
        userManager.Setup(manager => manager.CheckPasswordAsync(user, Password)).ReturnsAsync(true);
        userManager.Setup(manager => manager.ResetAccessFailedCountAsync(user)).ReturnsAsync(IdentityResult.Success);
        userManager.Setup(manager => manager.GetRolesAsync(user)).ReturnsAsync([ApplicationRoles.Client]);
        var service = new ApplicationUserAuthenticationService(userManager.Object);

        var result = await service.AuthenticateAsync(Email, Password, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
        Assert.Equal(Email, result.Email);
        Assert.Equal(LanguageEnum.en, result.Language);
        Assert.Contains(ApplicationRoles.Client, result.Roles);
        userManager.Verify(manager => manager.ResetAccessFailedCountAsync(user), Times.Once);
        userManager.Verify(manager => manager.AccessFailedAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task AuthenticateAsync_RegistersFailedAttemptForInvalidPassword()
    {
        var user = CreateUser();
        var userManager = CreateUserManager();
        userManager.Setup(manager => manager.FindByEmailAsync(Email)).ReturnsAsync(user);
        userManager.Setup(manager => manager.IsLockedOutAsync(user)).ReturnsAsync(false);
        userManager.Setup(manager => manager.CheckPasswordAsync(user, Password)).ReturnsAsync(false);
        userManager.Setup(manager => manager.AccessFailedAsync(user)).ReturnsAsync(IdentityResult.Success);
        var service = new ApplicationUserAuthenticationService(userManager.Object);

        var result = await service.AuthenticateAsync(Email, Password, CancellationToken.None);

        Assert.Null(result);
        userManager.Verify(manager => manager.AccessFailedAsync(user), Times.Once);
        userManager.Verify(manager => manager.ResetAccessFailedCountAsync(It.IsAny<ApplicationUser>()), Times.Never);
        userManager.Verify(manager => manager.GetRolesAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task AuthenticateAsync_DoesNotCheckPasswordForLockedUser()
    {
        var user = CreateUser();
        var userManager = CreateUserManager();
        userManager.Setup(manager => manager.FindByEmailAsync(Email)).ReturnsAsync(user);
        userManager.Setup(manager => manager.IsLockedOutAsync(user)).ReturnsAsync(true);
        var service = new ApplicationUserAuthenticationService(userManager.Object);

        var result = await service.AuthenticateAsync(Email, Password, CancellationToken.None);

        Assert.Null(result);
        userManager.Verify(manager => manager.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        userManager.Verify(manager => manager.AccessFailedAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task FindActiveByIdAsync_ReturnsNullForUnknownUser()
    {
        var userManager = CreateUserManager();
        userManager.Setup(manager => manager.FindByIdAsync("missing-user")).ReturnsAsync((ApplicationUser?)null);
        var service = new ApplicationUserAuthenticationService(userManager.Object);

        var result = await service.FindActiveByIdAsync("missing-user", CancellationToken.None);

        Assert.Null(result);
        userManager.Verify(manager => manager.IsLockedOutAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    private static ApplicationUser CreateUser()
    {
        return new ApplicationUser
        {
            Id = "user-id",
            Email = Email,
            UserName = Email,
            Language = LanguageEnum.en
        };
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
}
