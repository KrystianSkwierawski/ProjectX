using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ProjectX.Domain.Constants;
using ProjectX.Domain.Entities;
using ProjectX.Infrastructure.Identity;

namespace ProjectX.Infrastructure.Persistance;
public static class InitialiserExtensions
{
    public static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();

        await service.InitialiseAsync();
    }
}

public class ApplicationDbContextInitialiser
{
    private static readonly Serilog.ILogger Log = Serilog.Log.ForContext(typeof(ApplicationDbContextInitialiser));

    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public ApplicationDbContextInitialiser(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task InitialiseAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.Database.EnsureCreatedAsync();

        await _roleManager.CreateAsync(new IdentityRole(Roles.Server));
        await _roleManager.CreateAsync(new IdentityRole(Roles.Client));

        await CreateUserAsync("administrator1@localhost", "Administrator1!", Roles.Server);
        await CreateUserAsync("administrator2@localhost", "Administrator2!", Roles.Server);
        await CreateUserAsync("user1@localhost", "User1!", Roles.Client);
        await CreateUserAsync("user2@localhost", "User2!", Roles.Client);
    }

    private async Task CreateUserAsync(string userName, string password, string role)
    {
        var user = new ApplicationUser { UserName = userName, Email = userName };

        if (_userManager.Users.All(u => u.UserName != user.UserName))
        {
            await _userManager.CreateAsync(user, password);
            await _userManager.AddToRolesAsync(user, [role]);

            var character = new Character
            {
                ModDate = DateTime.Now,
                ApplicationUserId = user.Id
            };

            var characterPosition = new CharacterPosition
            {
                X = 0,
                Y = 0,
                Z = 0,
                ModDate = DateTime.Now,
                Character = character,
            };

            _context.Character.Add(character);
            _context.CharacterPosition.Add(characterPosition);

            await _context.SaveChangesAsync();
        }
    }
}
