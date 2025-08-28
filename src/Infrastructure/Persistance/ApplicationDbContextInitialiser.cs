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
        Log.Information("InitialiseAsync -> Start");

        //await _context.Database.EnsureDeletedAsync();
        //Log.Debug("InitialiseAsync -> Ensured deleted database");

        await _context.Database.EnsureCreatedAsync();
        Log.Debug("InitialiseAsync -> Ensured created database");

        await CreateRoleAsync(Roles.Server);
        await CreateRoleAsync(Roles.Client);

        await CreateUserAsync("server1@localhost", "Server1!", Roles.Server);
        await CreateUserAsync("server2@localhost", "Server1!", Roles.Server);
        await CreateUserAsync("user1@localhost", "User1!", Roles.Client);
        await CreateUserAsync("user2@localhost", "User2!", Roles.Client);

        Log.Information("InitialiseAsync -> Stop");
    }

    private async Task CreateRoleAsync(string role)
    {
        if (_roleManager.Roles.All(r => r.Name != role))
        {
            await _roleManager.CreateAsync(new IdentityRole(role));
            Log.Debug("CreateRoleAsync -> Created role. Name: {0}", role);
        }
    }

    private async Task CreateUserAsync(string userName, string password, string role)
    {
        if (_userManager.Users.All(u => u.UserName != userName))
        {
            var user = new ApplicationUser { UserName = userName, Email = userName };

            await _userManager.CreateAsync(user, password);
            await _userManager.AddToRolesAsync(user, [role]);

            var character = new Character
            {
                ModDate = DateTime.Now,
                ApplicationUserId = user.Id
            };

            var characterPosition = new CharacterPosition
            {
                X = 1,
                Y = 0,
                Z = 0,
                ModDate = DateTime.Now,
                Character = character,
            };

            _context.Character.Add(character);
            _context.CharacterPosition.Add(characterPosition);

            await _context.SaveChangesAsync();

            Log.Debug("CreateUserAsync -> Created user. UserName: {0}, Role: {1}, CharacterId: {2}, CharacterPositionId: {3}", user, role, character.Id, characterPosition.Id);
        }
    }
}
